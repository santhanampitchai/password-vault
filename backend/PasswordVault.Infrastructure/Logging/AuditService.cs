using Microsoft.Extensions.Logging;
using PasswordVault.Application.Interfaces;
using PasswordVault.Domain.Interfaces;

namespace PasswordVault.Infrastructure.Logging;

/// <summary>
/// Logs password reveal events via Serilog structured logging.
/// Extend to write to a DB table for compliance/audit trails.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IUnitOfWork _uow;

    public AuditService(ILogger<AuditService> logger, IUnitOfWork uow)
    {
        _logger = logger;
        _uow    = uow;
    }

    public async Task LogPasswordRevealAsync(
        int accountId, int userId, string ipAddress, CancellationToken ct = default)
    {
        var user    = await _uow.Users.GetByIdAsync(userId, ct);
        var account = await _uow.Accounts.GetByIdAsync(accountId, userId, ct);

        _logger.LogWarning(
            "PASSWORD_REVEAL | AccountId={AccountId} AccountName={AccountName} " +
            "UserId={UserId} UserEmail={Email} IP={IP} At={At}",
            accountId,
            account?.AccountName ?? "?",
            userId,
            user?.Email ?? "?",
            ipAddress,
            DateTime.UtcNow);
    }
}
