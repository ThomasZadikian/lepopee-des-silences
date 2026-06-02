using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Items;

public interface IItemTemplate : ICatalogContent
{
    ItemCategory Category { get; }

    ItemRarity Rarity { get; }

    ItemDuration Duration { get; }

    int EffectValue { get; }

    int Price { get; }
}