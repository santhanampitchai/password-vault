using Microsoft.Extensions.Logging;
using PasswordVault.Application.DTOs;
using PasswordVault.Application.Interfaces;
using PasswordVault.Domain.Entities;
using PasswordVault.Domain.Interfaces;

namespace PasswordVault.Application.Services;

/// <summary>
/// Handles user registration, login, and JWT refresh.
/// Refresh tokens are stored in-memory for this implementation;
/// replace with a DB table (RefreshTokens) for production.
/// </summary>
public sealed class AuthService : IAuthService
{
    // In-memory refresh token store. Replace with DB-backed store in production.
    private static readonly Dictionary<string, (int UserId, DateTime Expiry)> _refreshTokens = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _hasher = hasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _uow.Users.ExistsAsync(request.Email, ct))
            throw new InvalidOperationException("An account with this email already exists.");

        var (hash, salt) = _hasher.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedDate = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email}", user.Email);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        if (!_hasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedAccessException("Invalid email or password.");

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_refreshTokens.TryGetValue(refreshToken, out var entry) || entry.Expiry < DateTime.UtcNow)
            {
                _refreshTokens.Remove(refreshToken);
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var user = await _uow.Users.GetByIdAsync(entry.UserId, ct)
                ?? throw new UnauthorizedAccessException("User not found.");

            _refreshTokens.Remove(refreshToken);
            return BuildAuthResponse(user);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try { _refreshTokens.Remove(refreshToken); }
        finally { _lock.Release(); }
    }

    // ─── Private ───────────────────────────────────────────────────────────

    private AuthResponse BuildAuthResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.UserId, user.Email, user.FullName);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddHours(1);

        _refreshTokens[refreshToken] = (user.UserId, DateTime.UtcNow.AddDays(7));

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiry,
            new UserDto(user.UserId, user.FullName, user.Email));
    }
}
