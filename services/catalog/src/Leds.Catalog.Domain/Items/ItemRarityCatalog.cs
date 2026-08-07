using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Items;

/// <summary>
/// Canonical Catalog-owned vocabulary for item rarities — same pattern as
/// <see cref="ItemTypeCatalog"/>/<c>EmotionalRegisterCatalog</c>. Folds in the pricing
/// table (<see cref="ItemPricing"/>) so shard costs travel with the same definition
/// that carries the label/glyph/color — one record per rarity, not two lookups that
/// have to be kept in sync by hand.
/// </summary>
public static class ItemRarityCatalog
{
    public const string Version = "item-rarities-1.0.0";

    private static readonly IReadOnlyList<ItemRarityDefinition> Definitions = BuildDefinitions();

    private static IReadOnlyList<ItemRarityDefinition> BuildDefinitions()
    {
        (string Code, string DisplayName, string Glyph, string Color, ItemRarity Value)[] raw =
        [
            ("common", "Commune", "○", "oklch(0.65 0.02 270)", ItemRarity.Common),
            ("uncommon", "Peu commune", "◇", "oklch(0.81 0.145 150)", ItemRarity.Uncommon),
            ("rare", "Rare", "◈", "oklch(0.81 0.145 230)", ItemRarity.Rare),
            ("epic", "Épique", "❖", "oklch(0.81 0.174 300)", ItemRarity.Epic),
            ("legendary", "Légendaire", "✶", "oklch(0.86 0.174 85)", ItemRarity.Legendary),
            ("unique", "Unique", "✺", "oklch(0.81 0.18 20)", ItemRarity.Unique)
        ];

        return raw
            .Select(r =>
            {
                var (palaceShardCost, himLitShardCost) = ItemPricing.ForRarity(r.Value);
                return new ItemRarityDefinition(
                    r.Code, r.DisplayName, r.Glyph, r.Color, r.Value, palaceShardCost, himLitShardCost);
            })
            .ToArray();
    }

    private static readonly IReadOnlyDictionary<string, ItemRarityDefinition> ByCode =
        Definitions.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<ItemRarity, ItemRarityDefinition> ByValue =
        Definitions.ToDictionary(d => d.Value);

    public static IReadOnlyList<ItemRarityDefinition> All => Definitions;

    public static ItemRarity Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Item rarity is required.");

        var normalized = value.Trim();
        if (ByCode.TryGetValue(normalized, out var byCode))
            return byCode.Value;

        var byName = Definitions.FirstOrDefault(d =>
            string.Equals(d.Value.ToString(), normalized, StringComparison.OrdinalIgnoreCase));

        return byName?.Value
            ?? throw new DomainException($"Unknown item rarity '{value}'.");
    }

    public static bool TryParse(string? value, out ItemRarity rarity)
    {
        try
        {
            rarity = Parse(value ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            rarity = default;
            return false;
        }
    }

    public static string CodeOf(ItemRarity rarity) =>
        ByValue.TryGetValue(rarity, out var definition)
            ? definition.Code
            : throw new DomainException($"Unknown item rarity value '{rarity}'.");
}

public sealed record ItemRarityDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    ItemRarity Value,
    int PalaceShardCost,
    int HimLitShardCost);
