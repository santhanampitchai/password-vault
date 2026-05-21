namespace PasswordVault.Domain.Entities;

/// <summary>
/// Represents a stored account credential.
/// </summary>
public class Account
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>AES-256 encrypted password (Base64).</summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>AES-256 encrypted additional info (Base64), nullable.</summary>
    public string? EncryptedOtherInfo { get; set; }

    public string? Category { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>Base64-encoded IV used during encryption.</summary>
    public string EncryptionIV { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
