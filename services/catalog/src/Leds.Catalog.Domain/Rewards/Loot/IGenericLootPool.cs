using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Rewards.Loot;

public interface IGenericLootPool : ICatalogContent
{
    IReadOnlyCollection<LootEntry> Entries { get; }
}
