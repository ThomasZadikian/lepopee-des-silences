namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class PalaceIndicatorEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string IndicatorKey { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;
    public string Intensity { get; set; } = string.Empty;
    public Guid? SourceDecisionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public RunEntity? Run { get; set; }
}
