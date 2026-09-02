using System.Security.Cryptography;
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

        if (!_logger.IsEnabled(LogLevel.Information))
            return Task.CompletedTask;

        var accountReference = accountId.HasValue
            ? Convert.ToHexString(SHA256.HashData(accountId.Value.ToByteArray()))[..16]
            : "anonymous";
        _logger.LogInformation(
            "Account security event {EventType} for account reference {AccountReference} at {OccurredAtUtc}",
            eventType,
            accountReference,
            occurredAtUtc);
        return Task.CompletedTask;
    }
}
