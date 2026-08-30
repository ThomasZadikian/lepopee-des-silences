namespace Leds.Catalog.Domain.Items;

public static class ItemPricing
{
    public static (int PalaceShardCost, int HimLitShardCost) ForRarity(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => (150, 0),
        ItemRarity.Uncommon => (250, 0),
        ItemRarity.Rare => (350, 0),
        ItemRarity.Epic => (500, 25),
        ItemRarity.Legendary => (750, 50),
        ItemRarity.Unique => (1000, 75),
        _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown item rarity.")
    };

    public static (int PalaceShardCost, int HimLitShardCost) ForRarity(string rarity)
        => ForRarity(Enum.Parse<ItemRarity>(rarity, ignoreCase: true));
}
