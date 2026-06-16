namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class AdaptiveInfluenceEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string InfluenceType { get; set; } = string.Empty;
    public string InfluenceTag { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string? ValueMode { get; set; }
    public string Duration { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
}
