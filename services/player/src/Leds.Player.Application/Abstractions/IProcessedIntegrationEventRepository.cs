namespace Leds.Player.Application.Abstractions;

public interface IProcessedIntegrationEventRepository
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, string type, DateTime processedAtUtc, CancellationToken cancellationToken);
}
