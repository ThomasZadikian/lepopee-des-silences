using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Enemies.Loot.GetEnemyLootTableByKey;

public sealed record GetEnemyLootTableByKeyQuery(string EnemyKey)
    : IQuery<GetEnemyLootTableByKeyResponse>;
