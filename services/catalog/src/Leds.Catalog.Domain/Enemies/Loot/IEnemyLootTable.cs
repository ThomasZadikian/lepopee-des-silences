using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.Domain.Enemies.Loot;

public interface IEnemyLootTable : ICatalogContent
{
    string EnemyDefinitionKey { get; }

    IReadOnlyCollection<LootEntry> Entries { get; }
}
