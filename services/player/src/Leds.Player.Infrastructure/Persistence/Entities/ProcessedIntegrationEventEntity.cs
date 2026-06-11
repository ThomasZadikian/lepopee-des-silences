namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class ProcessedIntegrationEventEntity
{
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
