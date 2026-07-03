using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Application.Enemies.Loot.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies.Loot;
using Leds.Catalog.Domain.Rewards.Loot;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfEnemyLootTableReadStore : IEnemyLootTableReadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public EfEnemyLootTableReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnemyLootTable?> GetByEnemyKeyAsync(
        string enemyDefinitionKey,
        CancellationToken cancellationToken)
    {
        var entity = await _context.EnemyLootTables
            .FirstOrDefaultAsync(e => e.EnemyDefinitionKey == enemyDefinitionKey && e.Status == "Active", cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IEnemyLootTable MapToDomain(EnemyLootTableEntity entity)
    {
        var entries = JsonSerializer.Deserialize<List<LootEntry>>(entity.EntriesJson, JsonOptions) ?? [];

        return EnemyLootTable.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            entity.EnemyDefinitionKey,
            entries,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
