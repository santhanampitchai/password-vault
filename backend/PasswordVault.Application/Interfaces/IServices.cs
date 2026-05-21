using PasswordVault.Application.DTOs;

namespace PasswordVault.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<PagedResult<AccountDto>> GetAccountsAsync(int userId, AccountQueryParams query, CancellationToken ct = default);
    Task<AccountDetailDto> GetAccountByIdAsync(int accountId, int userId, CancellationToken ct = default);
    Task<AccountDetailDto> CreateAccountAsync(int userId, CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountDetailDto> UpdateAccountAsync(int accountId, int userId, UpdateAccountRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(int accountId, int userId, CancellationToken ct = default);
    Task<AccountDetailDto> GetDecryptedAccountAsync(int accountId, int userId, string ipAddress, CancellationToken ct = default);
}

public interface IEncryptionService
{
    /// <summary>Encrypts plaintext using AES-256-CBC; returns (ciphertext, iv) both Base64.</summary>
    (string CipherText, string IV) Encrypt(string plainText);

    /// <summary>Decrypts Base64 ciphertext using the given Base64 IV.</summary>
    string Decrypt(string cipherText, string iv);
}

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string fullName);
    string GenerateRefreshToken();
    int? ValidateAccessToken(string token);
}

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public interface IAuditService
{
    Task LogPasswordRevealAsync(int accountId, int userId, string ipAddress, CancellationToken ct = default);
}
