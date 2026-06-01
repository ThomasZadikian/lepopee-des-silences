using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Items;

public sealed class ItemTemplate : CatalogContentBase
{
    private ItemTemplate(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        ItemCategory category,
        ItemRarity rarity,
        ItemDuration duration,
        int effectValue,
        int price)
        : base(id, key, name, description, version, status)
    {
        Category = category;
        Rarity = rarity;
        Duration = duration;
        EffectValue = effectValue;
        Price = price;
    }

    public ItemCategory Category { get; }

    public ItemRarity Rarity { get; }

    public ItemDuration Duration { get; }

    public int EffectValue { get; }

    public int Price { get; }

    public static ItemTemplate Create(
        string key,
        string name,
        string? description,
        string version,
        ItemCategory category,
        ItemRarity rarity,
        ItemDuration duration,
        int effectValue,
        int price,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (effectValue < 0)
        {
            throw new DomainException("Item effect value cannot be negative.");
        }

        if (price < 0)
        {
            throw new DomainException("Item price cannot be negative.");
        }

        return new ItemTemplate(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            category,
            rarity,
            duration,
            effectValue,
            price);
    }
}