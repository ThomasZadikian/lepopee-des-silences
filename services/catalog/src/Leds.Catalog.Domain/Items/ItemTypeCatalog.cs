using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Items;

/// <summary>
/// Canonical Catalog-owned vocabulary for item categories — mirrors
/// <c>EmotionalRegisterCatalog</c>: the display metadata (label/glyph/color) for
/// every <see cref="ItemCategory"/> value lives here, once, and every consumer
/// (game-engine's runtime mapper, the frontend's item-vocabulary store) resolves
/// against this single list instead of hand-authoring its own.
/// </summary>
public static class ItemTypeCatalog
{
    public const string Version = "item-types-1.0.0";

    private static readonly IReadOnlyList<ItemTypeDefinition> Definitions =
    [
        new("consumable", "Consommable", "✳", "oklch(0.83 0.10 150)", ItemCategory.Consumable),
        new("equipment", "Équipement", "◆", "oklch(0.82 0.09 255)", ItemCategory.Equipment),
        new("relic", "Relique", "◈", "oklch(0.82 0.10 305)", ItemCategory.Relic),
        new("key", "Clé", "⚷", "oklch(0.83 0.08 220)", ItemCategory.Key),
        new("currency", "Monnaie", "○", "oklch(0.85 0.11 60)", ItemCategory.Currency),
        new("material", "Matériau", "▲", "oklch(0.75 0.06 130)", ItemCategory.Material),
        new("weapon", "Arme", "⚔", "oklch(0.80 0.12 30)", ItemCategory.Weapon),
        new("grimoire", "Grimoire", "❍", "oklch(0.86 0.09 90)", ItemCategory.Grimoire),
        new("weatherinstrument", "Instrument météorologique", "❋", "oklch(0.84 0.09 190)", ItemCategory.WeatherInstrument),
        new("skillessence", "Essence de sort", "✶", "oklch(0.82 0.12 335)", ItemCategory.SkillEssence)
    ];

    private static readonly IReadOnlyDictionary<string, ItemTypeDefinition> ByCode =
        Definitions.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<ItemCategory, ItemTypeDefinition> ByValue =
        Definitions.ToDictionary(d => d.Value);

    public static IReadOnlyList<ItemTypeDefinition> All => Definitions;

    public static ItemCategory Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Item category is required.");

        var normalized = value.Trim();
        if (ByCode.TryGetValue(normalized, out var byCode))
            return byCode.Value;

        var byName = Definitions.FirstOrDefault(d =>
            string.Equals(d.Value.ToString(), normalized, StringComparison.OrdinalIgnoreCase));

        return byName?.Value
            ?? throw new DomainException($"Unknown item category '{value}'.");
    }

    public static bool TryParse(string? value, out ItemCategory category)
    {
        try
        {
            category = Parse(value ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            category = default;
            return false;
        }
    }

    public static string CodeOf(ItemCategory category) =>
        ByValue.TryGetValue(category, out var definition)
            ? definition.Code
            : throw new DomainException($"Unknown item category value '{category}'.");
}

public sealed record ItemTypeDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    ItemCategory Value);
