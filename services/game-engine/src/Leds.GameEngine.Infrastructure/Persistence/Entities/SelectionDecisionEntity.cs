namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class SelectionDecisionEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string DecisionType { get; set; } = string.Empty;
    public string? ContextKey { get; set; }
    public string SelectedKey { get; set; } = string.Empty;
    public string? SelectionGroup { get; set; }
    public string Seed { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? MatrixVersion { get; set; }
    public string? InfluenceSummaryKey { get; set; }

    public RunEntity? Run { get; set; }
}
