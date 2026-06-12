namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunModifierEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
}
