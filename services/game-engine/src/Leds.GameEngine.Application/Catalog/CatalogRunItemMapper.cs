using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Catalog;

/// <summary>
/// Explicit anti-corruption mapping from Catalog-authored item vocabulary to the
/// smaller runtime model. Every method reads exactly one Catalog field — never a
/// second field consulted as a fallback — and either returns a value from the total,
/// documented table below or throws. Unknown values are contract errors, never
/// gameplay defaults.
/// </summary>
public static class CatalogRunItemMapper
{
    /// <summary>
    /// Resolves a catalog item's runtime kind from its single authored classification
    /// field, <c>category</c> (Catalog's <c>ItemCategory</c>). Most category names are
    /// also <see cref="RunItemType"/> members and parse directly; Category is an
    /// authoring-time vocabulary and stays intentionally larger than the runtime one
    /// for the handful of values with no distinct runtime behavior of their own — see
    /// the collapse table below, which is total and never falls through to a second
    /// field.
    /// </summary>
    public static RunItemType MapType(string category)
    {
        if (Enum.TryParse<RunItemType>(category, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        return category switch
        {
            // Key/Material items behave identically to any other non-equippable,
            // non-consumable item at runtime — no dedicated RunItemType earns its own
            // member for them.
            "Key" or "Material" => RunItemType.Passive,
            // A Currency-category catalog item has no run-inventory representation
            // (tracked as a balance, never as a RunItem) — reaching this mapper for
            // one is a contract error, not a gap to paper over.
            _ => throw new DomainException(
                $"Catalog item category '{category}' has no runtime representation.")
        };
    }

    /// <summary>
    /// Resolves a catalog item's runtime rarity from its single authored <c>rarity</c>
    /// field. <see cref="RunItemRarity"/> has no 6th "Unique" tier — Catalog's Unique
    /// collapses to Legendary, the same total-table pattern as <see cref="MapType"/>.
    /// </summary>
    public static RunItemRarity MapRarity(string rarity)
    {
        if (Enum.TryParse<RunItemRarity>(rarity, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        return rarity switch
        {
            "Unique" => RunItemRarity.Legendary,
            _ => throw new DomainException(
                $"Catalog item rarity '{rarity}' is not supported by the runtime.")
        };
    }

    /// <summary>
    /// Non-throwing variant of <see cref="MapType"/> for display-only call sites (e.g.
    /// listing every active item to show its equip slot) where a category with no
    /// runtime representation — Currency — is an expected, silent "not applicable"
    /// rather than a contract error. Combat/loot/reward resolution must keep calling
    /// the throwing <see cref="MapType"/>: reaching an unmappable category there means
    /// corrupted data actually entering gameplay.
    /// </summary>
    public static bool TryMapType(string category, out RunItemType type)
    {
        try
        {
            type = MapType(category);
            return true;
        }
        catch (DomainException)
        {
            type = default;
            return false;
        }
    }

    /// <summary>
    /// Resolves the equipment slot a <see cref="RunItemType"/> belongs in, or
    /// <c>null</c> if that kind is not equippable at all (Consumable/Passive/
    /// Fragment). The single place this decision is made — <see cref="MapType"/>'s
    /// result flows straight in, both for the actual equip command
    /// (EquipItemCommandHandler) and for the <c>equipSlot</c> exposed on item
    /// listings so the frontend never has to re-derive it from raw category/type
    /// fields.
    /// </summary>
    public static string? MapEquipSlot(RunItemType type) => type switch
    {
        RunItemType.Weapon => "Weapon",
        RunItemType.Equipment => "Accessory",
        RunItemType.Relic or RunItemType.Grimoire or RunItemType.WeatherInstrument or RunItemType.SkillEssence
            => "Relic",
        _ => null
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
