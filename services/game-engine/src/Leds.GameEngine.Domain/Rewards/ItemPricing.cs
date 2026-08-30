namespace Leds.GameEngine.Domain.Rewards;

/// <summary>
/// Fixed price per item rarity tier, in the two Palace currencies. Mirrors
/// Leds.Catalog.Domain.Items.ItemPricing exactly (same fixed values) — duplicated
/// rather than shared because Catalog and game-engine are separate deployables;
/// keeping a small parallel table here avoids a cross-service dependency for a
/// handful of constants that only ever change together.
/// </summary>
public static class ItemPricing
{
    public static (int PalaceShardCost, int HimLitShardCost) ForRarity(string? rarity) =>
        rarity?.Trim().ToLowerInvariant() switch
        {
            "common" => (150, 0),
            "uncommon" => (250, 0),
            "rare" => (350, 0),
            "epic" => (500, 25),
            "legendary" => (750, 50),
            "unique" => (1000, 75),
            // Unknown/missing rarity (e.g. legacy reward template options authored
            // before ItemRarity existed) default to the Common tier price.
            _ => (150, 0)
        };
}
