namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string EventVersion { get; set; } = "v1";
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public string? Destination { get; set; }
}