using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Catalog;

/// <summary>
/// Explicit anti-corruption mapping from Catalog-authored item vocabulary to the
/// smaller runtime model. Unknown values are contract errors, never gameplay defaults.
/// </summary>
public static class CatalogRunItemMapper
{
    /// <summary>
    /// Resolves a catalog item's runtime kind from its two authored fields:
    /// <c>itemType</c> — a free-form narrative/flavor subtype (e.g. "Lore", "Potion",
    /// "Trophy", "Light", "Heal"; see <c>CatalogSeedRunner.UpsertItemAsync</c> callers)
    /// — and <c>category</c>, the small, closed <c>ItemCategory</c> vocabulary
    /// (Consumable/Equipment/Relic/Key/Currency/Material) that actually determines
    /// runtime behavior. A hardcoded switch over every possible itemType flavor used to
    /// live here and rot every time a new one was authored (that's exactly how "Light"/
    /// "Heal"/"Trophy"/... items started throwing at combat-end loot resolution — the
    /// switch simply never learned about them). itemType is only consulted for the
    /// handful of RunItemType members it can name directly (a weapon or grimoire
    /// carved out of an otherwise generic Equipment/Relic category); category is the
    /// authoritative fallback for everything else, matching how the catalog itself
    /// models items.
    /// </summary>
    public static RunItemType MapType(string itemType, string category)
    {
        if (Enum.TryParse<RunItemType>(itemType, ignoreCase: true, out var byItemType)
            && Enum.IsDefined(byItemType))
        {
            return byItemType;
        }

        return category switch
        {
            "Consumable" => RunItemType.Consumable,
            "Equipment" => RunItemType.Equipment,
            "Relic" => RunItemType.Relic,
            "Key" or "Material" => RunItemType.Passive,
            // "Currency" intentionally excluded: a currency-category catalog item has no
            // run-inventory representation (tracked as a balance, never as a RunItem) —
            // reaching this mapper for one is a contract error, not a gap to paper over.
            _ => throw new DomainException(
                $"Catalog item category '{category}' (type '{itemType}') is not supported by the runtime.")
        };
    }

    public static RunItemRarity MapRarity(string rarity) => rarity switch
    {
        "Common" => RunItemRarity.Common,
        "Uncommon" => RunItemRarity.Uncommon,
        "Rare" => RunItemRarity.Rare,
        "Epic" => RunItemRarity.Epic,
        "Legendary" or "Unique" => RunItemRarity.Legendary,
        _ => throw new DomainException(
            $"Catalog item rarity '{rarity}' is not supported by the runtime.")
    };

    public static RunItemEffectType MapEffect(string? effectRunType)
    {
        if (string.IsNullOrWhiteSpace(effectRunType))
            return RunItemEffectType.None;

        return Enum.TryParse<RunItemEffectType>(effectRunType, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException(
                    $"Catalog item effect '{effectRunType}' is not supported by the runtime.");
    }
}
