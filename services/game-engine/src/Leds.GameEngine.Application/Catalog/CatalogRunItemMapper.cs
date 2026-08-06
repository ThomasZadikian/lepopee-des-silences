using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Catalog;

/// <summary>
/// Explicit anti-corruption mapping from Catalog-authored item vocabulary to the
/// smaller runtime model. Unknown values are contract errors, never gameplay defaults.
/// </summary>
public static class CatalogRunItemMapper
{
    public static RunItemType MapType(string itemType, string category) => itemType switch
    {
        "Potion" or "Consumable" => RunItemType.Consumable,
        "Accessory" or "Equipment" => RunItemType.Equipment,
        "Relic" or "Heritage" => RunItemType.Relic,
        "Grimoire" => RunItemType.Grimoire,
        "WeatherInstrument" => RunItemType.WeatherInstrument,
        "SkillEssence" => RunItemType.SkillEssence,
        "Weapon" => RunItemType.Weapon,
        "Key" or "Material" or "Container" => RunItemType.Passive,
        _ when string.Equals(category, "Relic", StringComparison.OrdinalIgnoreCase)
            => RunItemType.Relic,
        _ => throw new DomainException(
            $"Catalog item type '{itemType}' (category '{category}') is not supported by the runtime.")
    };

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
