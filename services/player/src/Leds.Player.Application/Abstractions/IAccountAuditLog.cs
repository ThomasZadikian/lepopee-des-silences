namespace Leds.Player.Application.Abstractions;

public interface IAccountAuditLog
{
    Task WriteAsync(
        Guid? accountId,
        string eventType,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken);
}

internal sealed class NullAccountAuditLog : IAccountAuditLog
{
    public Task WriteAsync(
        Guid? accountId,
        string eventType,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
