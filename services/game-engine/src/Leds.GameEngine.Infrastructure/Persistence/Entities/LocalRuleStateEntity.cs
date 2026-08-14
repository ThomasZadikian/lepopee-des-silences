namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class LocalRuleStateEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string LocalRuleKey { get; set; } = string.Empty;
    public int CumulativeSeverity { get; set; }
    public bool HasBeenInformed { get; set; }
    /// <summary>Comma-separated severity thresholds already triggered, e.g. "1,2".</summary>
    public string TriggeredThresholdsCsv { get; set; } = string.Empty;

    public RoomEntity? Room { get; set; }
}
