namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunItemEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string DefinitionKey { get; set; } = string.Empty;
    public string? DefinitionVersion { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int Quantity { get; set; }
    public int? MaxStack { get; set; }
    public string? UsageMode { get; set; }
    public string? Lifecycle { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public int EffectAmount { get; set; }
    public string? EffectSetKey { get; set; }
    public string? EffectSummary { get; set; }
    public bool? IsUsableInCombat { get; set; }
    public bool? IsUsableOutsideCombat { get; set; }
    public Guid? SourceRewardOptionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
}
