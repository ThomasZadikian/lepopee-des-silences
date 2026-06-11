using Leds.Player.Application.Abstractions;

namespace Leds.Player.Infrastructure.Persistence.InMemory;

public sealed class InMemoryProcessedIntegrationEventRepository : IProcessedIntegrationEventRepository
{
    private readonly Dictionary<Guid, (string Type, DateTime ProcessedAtUtc)> _processedEvents = [];

    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_processedEvents.ContainsKey(eventId));
    }

    public Task MarkProcessedAsync(Guid eventId, string type, DateTime processedAtUtc, CancellationToken cancellationToken)
    {
        _processedEvents[eventId] = (type, processedAtUtc);
        return Task.CompletedTask;
    }
}
