namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class ItemDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string UsageMode { get; set; } = "NotUsable";
    public string Lifecycle { get; set; } = "RuntimeRunOnly";
    public string StackPolicy { get; set; } = "Additive";
    public int MaxStack { get; set; } = 1;
    public bool IsUsableInCombat { get; set; }
    public bool IsUsableOutsideCombat { get; set; }
    public Guid? EffectSetId { get; set; }
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string Duration { get; set; } = string.Empty;
    public int EffectValue { get; set; }
    public int Price { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<ItemTagEntity> Tags { get; set; } = [];
}
