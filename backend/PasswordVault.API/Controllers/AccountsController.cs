using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordVault.Application.DTOs;
using PasswordVault.Application.Interfaces;

namespace PasswordVault.API.Controllers;

[ApiController]
[Route("api/accounts")]
//[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AccountsController(IAccountService accounts) => _accounts = accounts;

    private int UserId =>
        int.Parse(User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

    /// <summary>Get paginated, filtered, sorted list of accounts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AccountDto>>), 200)]
    public async Task<IActionResult> GetAccounts([FromQuery] AccountQueryParams query, CancellationToken ct)
    {
        var result = await _accounts.GetAccountsAsync(UserId, query, ct);
        return Ok(new ApiResponse<PagedResult<AccountDto>>(true, null, result));
    }

    /// <summary>Get account metadata by ID (no encrypted payload).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAccount(int id, CancellationToken ct)
    {
        var result = await _accounts.GetAccountByIdAsync(id, UserId, ct);
        return Ok(new ApiResponse<AccountDetailDto>(true, null, result));
    }

    /// <summary>Create a new account credential.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), 201)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _accounts.CreateAccountAsync(UserId, request, ct);
        return CreatedAtAction(nameof(GetAccount), new { id = result.AccountId },
            new ApiResponse<AccountDetailDto>(true, "Account created.", result));
    }

    /// <summary>Update an existing account credential.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), 200)]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        var result = await _accounts.UpdateAccountAsync(id, UserId, request, ct);
        return Ok(new ApiResponse<AccountDetailDto>(true, "Account updated.", result));
    }

    /// <summary>Delete an account credential.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteAccount(int id, CancellationToken ct)
    {
        await _accounts.DeleteAccountAsync(id, UserId, ct);
        return NoContent();
    }

    /// <summary>
    /// Decrypt and return password + other info for display.
    /// Triggers audit log entry.
    /// </summary>
    [HttpPost("{id:int}/decrypt-password")]
    [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), 200)]
    public async Task<IActionResult> DecryptPassword(int id, CancellationToken ct)
    {
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _accounts.GetDecryptedAccountAsync(id, UserId, ip, ct);
        return Ok(new ApiResponse<AccountDetailDto>(true, null, result));
    }
}
