using System.ComponentModel.DataAnnotations;

namespace PasswordVault.Application.DTOs;

// ─── Auth ──────────────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RefreshTokenRequest([Required] string RefreshToken);

// ─── User ──────────────────────────────────────────────────────────────────

public record UserDto(int UserId, string FullName, string Email);

// ─── Account ───────────────────────────────────────────────────────────────

/// <summary>
/// Payload sent from Angular. Password and OtherInfo are AES-256 encrypted by the client.
/// </summary>
public record CreateAccountRequest(
    [Required, MaxLength(200)] string AccountName,
    [Required, MaxLength(300)] string UserName,

    /// <summary>Base64-encoded AES-256 ciphertext (client-encrypted).</summary>
    [Required] string EncryptedPassword,

    /// <summary>Base64-encoded IV used by the client.</summary>
    [Required] string ClientIV,

    string? EncryptedOtherInfo,
    [MaxLength(100)] string? Category,
    [MaxLength(500)] string? WebsiteUrl
);

public record UpdateAccountRequest(
    [Required, MaxLength(200)] string AccountName,
    [Required, MaxLength(300)] string UserName,
    [Required] string EncryptedPassword,
    [Required] string ClientIV,
    string? EncryptedOtherInfo,
    [MaxLength(100)] string? Category,
    [MaxLength(500)] string? WebsiteUrl
);

public record AccountDto(
    int AccountId,
    string AccountName,
    string UserName,
    string? Category,
    string? WebsiteUrl,
    DateTime CreatedDate,
    DateTime? UpdatedDate
);

public record AccountDetailDto(
    int AccountId,
    string AccountName,
    string UserName,
    string EncryptedPassword,
    string ClientIV,
    string? EncryptedOtherInfo,
    string? Category,
    string? WebsiteUrl,
    DateTime CreatedDate,
    DateTime? UpdatedDate
);

// ─── Pagination ────────────────────────────────────────────────────────────

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record AccountQueryParams(
    string? Search = null,
    string? Category = null,
    string? SortBy = "createdDate",
    string? SortDir = "desc",
    int Page = 1,
    int PageSize = 20
);

// ─── Audit ─────────────────────────────────────────────────────────────────

public record PasswordRevealAuditDto(
    int AccountId,
    string AccountName,
    string UserEmail,
    DateTime RevealedAt,
    string IpAddress
);

// ─── Common ────────────────────────────────────────────────────────────────

public record ApiResponse<T>(bool Success, string? Message, T? Data);
public record ApiError(string Code, string Message, IDictionary<string, string[]>? Errors = null);
