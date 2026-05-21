using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PasswordVault.Application.Interfaces;

namespace PasswordVault.Infrastructure.Security;

// ─── Encryption ────────────────────────────────────────────────────────────

public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<SecuritySettings> opts)
    {
        // Key must be 32 bytes for AES-256
        var raw = opts.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey not configured.");
        _key = DeriveKey(raw, 32);
    }

    public (string CipherText, string IV) Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key     = _key;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var enc    = aes.CreateEncryptor();
        var plainBytes   = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes  = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return (Convert.ToBase64String(cipherBytes), Convert.ToBase64String(aes.IV));
    }

    public string Decrypt(string cipherText, string iv)
    {
        using var aes = Aes.Create();
        aes.Key     = _key;
        aes.IV      = Convert.FromBase64String(iv);
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var dec   = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes  = dec.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string secret, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length == length) return bytes;
        using var sha = SHA256.Create();
        return sha.ComputeHash(bytes)[..length];
    }
}

// ─── Password Hasher ───────────────────────────────────────────────────────

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize       = 32;
    private const int HashSize       = 64;
    private const int Iterations     = 350_000;
    private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA512;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var salt      = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations, Algo, HashSize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes  = Convert.FromBase64String(salt);
        var hashBytes  = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), saltBytes, Iterations, Algo, HashSize);
        var storedHash = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(hashBytes, storedHash);
    }
}

// ─── Settings ──────────────────────────────────────────────────────────────

public sealed class SecuritySettings
{
    public string EncryptionKey { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = "PasswordVaultAPI";
    public string JwtAudience { get; set; } = "PasswordVaultClient";
    public int JwtExpiryMinutes { get; set; } = 60;
}
