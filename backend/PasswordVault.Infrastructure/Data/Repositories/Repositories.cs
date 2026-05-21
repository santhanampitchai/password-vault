using Microsoft.EntityFrameworkCore;
using PasswordVault.Domain.Entities;
using PasswordVault.Domain.Interfaces;

namespace PasswordVault.Infrastructure.Data.Repositories;

// ─── User Repository ───────────────────────────────────────────────────────

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(int userId, CancellationToken ct = default)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);
}

// ─── Account Repository ────────────────────────────────────────────────────

public sealed class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Account>> GetAllByUserAsync(int userId, CancellationToken ct = default)
        => await _db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

    public Task<Account?> GetByIdAsync(int accountId, int userId, CancellationToken ct = default)
        => _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == userId, ct);

    public async Task<Account> AddAsync(Account account, CancellationToken ct = default)
    {
        _db.Accounts.Add(account);
        return account;
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _db.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int accountId, int userId, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == userId, ct);
        if (account is not null)
            _db.Accounts.Remove(account);
    }

    public Task<int> CountByUserAsync(int userId, CancellationToken ct = default)
        => _db.Accounts.CountAsync(a => a.UserId == userId, ct);
}

// ─── Unit of Work ──────────────────────────────────────────────────────────

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public IUserRepository    Users    { get; }
    public IAccountRepository Accounts { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db      = db;
        Users    = new UserRepository(db);
        Accounts = new AccountRepository(db);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
