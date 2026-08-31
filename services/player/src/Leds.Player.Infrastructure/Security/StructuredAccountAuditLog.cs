using Leds.Player.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Leds.Player.Infrastructure.Security;

public sealed class StructuredAccountAuditLog : IAccountAuditLog
{
    private readonly ILogger<StructuredAccountAuditLog> _logger;

    public StructuredAccountAuditLog(ILogger<StructuredAccountAuditLog> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(
        Guid? accountId,
        string eventType,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "Account security event {EventType} for account {AccountId} at {OccurredAtUtc}",
            eventType,
            accountId,
            occurredAtUtc);
        return Task.CompletedTask;
    }
}
