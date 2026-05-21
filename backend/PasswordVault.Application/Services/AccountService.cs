using Microsoft.Extensions.Logging;
using PasswordVault.Application.DTOs;
using PasswordVault.Application.Interfaces;
using PasswordVault.Domain.Entities;
using PasswordVault.Domain.Interfaces;

namespace PasswordVault.Application.Services;

/// <summary>
/// Handles CRUD for account credentials.
/// The client sends AES-256 encrypted password; we store it as-is after
/// wrapping with server-side AES for defence-in-depth (double encryption).
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUnitOfWork uow,
        IEncryptionService encryption,
        IAuditService audit,
        ILogger<AccountService> logger)
    {
        _uow = uow;
        _encryption = encryption;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PagedResult<AccountDto>> GetAccountsAsync(
        int userId, AccountQueryParams query, CancellationToken ct = default)
    {
        var all = await _uow.Accounts.GetAllByUserAsync(userId, ct);

        // Filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            all = all.Where(a =>
                a.AccountName.ToLower().Contains(s) ||
                a.UserName.ToLower().Contains(s) ||
                (a.Category?.ToLower().Contains(s) ?? false) ||
                (a.WebsiteUrl?.ToLower().Contains(s) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
            all = all.Where(a => a.Category?.ToLower() == query.Category.ToLower());

        // Sort
        all = (query.SortBy?.ToLower(), query.SortDir?.ToLower()) switch
        {
            ("accountname", "asc")  => all.OrderBy(a => a.AccountName),
            ("accountname", _)      => all.OrderByDescending(a => a.AccountName),
            ("username", "asc")     => all.OrderBy(a => a.UserName),
            ("username", _)         => all.OrderByDescending(a => a.UserName),
            ("category", "asc")     => all.OrderBy(a => a.Category),
            ("category", _)         => all.OrderByDescending(a => a.Category),
            ("createddate", "asc")  => all.OrderBy(a => a.CreatedDate),
            _                       => all.OrderByDescending(a => a.CreatedDate)
        };

        var total = all.Count();
        var page  = Math.Max(1, query.Page);
        var size  = Math.Clamp(query.PageSize, 1, 100);

        var items = all
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToDto)
            .ToList();

        return new PagedResult<AccountDto>(items, total, page, size);
    }

    public async Task<AccountDetailDto> GetAccountByIdAsync(
        int accountId, int userId, CancellationToken ct = default)
    {
        var account = await GetOrThrowAsync(accountId, userId, ct);
        return ToDetailDto(account);
    }

    public async Task<AccountDetailDto> CreateAccountAsync(
        int userId, CreateAccountRequest request, CancellationToken ct = default)
    {
        // Re-encrypt with server key for double-layer storage
        var (serverCipher, serverIV) = _encryption.Encrypt(
            $"{request.ClientIV}:{request.EncryptedPassword}");

        string? serverOtherInfo = null;
        if (request.EncryptedOtherInfo is not null)
        {
            var (c, _) = _encryption.Encrypt(request.EncryptedOtherInfo);
            serverOtherInfo = c;
        }

        var account = new Account
        {
            UserId           = userId,
            AccountName      = request.AccountName,
            UserName         = request.UserName,
            EncryptedPassword = serverCipher,
            EncryptedOtherInfo = serverOtherInfo,
            Category         = request.Category,
            WebsiteUrl       = request.WebsiteUrl,
            EncryptionIV     = serverIV,
            CreatedDate      = DateTime.UtcNow
        };

        await _uow.Accounts.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Account created: {Name} for user {UserId}", account.AccountName, userId);
        return ToDetailDto(account);
    }

    public async Task<AccountDetailDto> UpdateAccountAsync(
        int accountId, int userId, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var account = await GetOrThrowAsync(accountId, userId, ct);

        var (serverCipher, serverIV) = _encryption.Encrypt(
            $"{request.ClientIV}:{request.EncryptedPassword}");

        string? serverOtherInfo = null;
        if (request.EncryptedOtherInfo is not null)
        {
            var (c, _) = _encryption.Encrypt(request.EncryptedOtherInfo);
            serverOtherInfo = c;
        }

        account.AccountName        = request.AccountName;
        account.UserName           = request.UserName;
        account.EncryptedPassword  = serverCipher;
        account.EncryptionIV       = serverIV;
        account.EncryptedOtherInfo = serverOtherInfo;
        account.Category           = request.Category;
        account.WebsiteUrl         = request.WebsiteUrl;
        account.UpdatedDate        = DateTime.UtcNow;

        await _uow.Accounts.UpdateAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Account updated: {Id}", accountId);
        return ToDetailDto(account);
    }

    public async Task DeleteAccountAsync(int accountId, int userId, CancellationToken ct = default)
    {
        await GetOrThrowAsync(accountId, userId, ct); // authorization check
        await _uow.Accounts.DeleteAsync(accountId, userId, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Account deleted: {Id}", accountId);
    }

    public async Task<AccountDetailDto> GetDecryptedAccountAsync(
        int accountId, int userId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetOrThrowAsync(accountId, userId, ct);
        await _audit.LogPasswordRevealAsync(accountId, userId, ipAddress, ct);

        // Unwrap server layer → returns "clientIV:clientCipher"
        var combined = _encryption.Decrypt(account.EncryptedPassword, account.EncryptionIV);
        var sep = combined.IndexOf(':');
        var clientIV     = combined[..sep];
        var clientCipher = combined[(sep + 1)..];

        string? otherInfo = null;
        if (account.EncryptedOtherInfo is not null)
            otherInfo = _encryption.Decrypt(account.EncryptedOtherInfo, account.EncryptionIV);

        // Return client IV + cipher so Angular can decrypt with its key
        return new AccountDetailDto(
                account.AccountId,
                account.AccountName,
                account.UserName,
                clientCipher,   // server layer unwrapped
                clientIV,
                otherInfo,
                account.Category,
                account.WebsiteUrl,
                account.CreatedDate,
                account.UpdatedDate);
    }

    // ─── Private ───────────────────────────────────────────────────────────

    private async Task<Account> GetOrThrowAsync(int accountId, int userId, CancellationToken ct)
    {
        return await _uow.Accounts.GetByIdAsync(accountId, userId, ct)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");
    }

    private static AccountDto ToDto(Account a) => new(
        a.AccountId, a.AccountName, a.UserName,
        a.Category, a.WebsiteUrl, a.CreatedDate, a.UpdatedDate);

    private static AccountDetailDto ToDetailDto(Account a) => new(
        a.AccountId, a.AccountName, a.UserName,
        a.EncryptedPassword, a.EncryptionIV,
        a.EncryptedOtherInfo, a.Category, a.WebsiteUrl,
        a.CreatedDate, a.UpdatedDate);
}
