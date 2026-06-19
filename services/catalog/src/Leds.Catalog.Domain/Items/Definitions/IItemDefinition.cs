using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Items.Definitions;

public interface IItemDefinition : ICatalogContent
{
    string? NarrativeText { get; }
    string Category { get; }
    string ItemType { get; }
    string Rarity { get; }
    string UsageMode { get; }
    string Lifecycle { get; }
    string StackPolicy { get; }
    int MaxStack { get; }
    bool IsUsableInCombat { get; }
    bool IsUsableOutsideCombat { get; }
    string? EffectSetKey { get; }
}
