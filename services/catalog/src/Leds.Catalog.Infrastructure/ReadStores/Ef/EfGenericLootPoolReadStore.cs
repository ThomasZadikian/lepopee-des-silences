using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Application.Rewards.GenericLoot.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Rewards.Loot;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfGenericLootPoolReadStore : IGenericLootPoolReadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public EfGenericLootPoolReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IGenericLootPool?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var entity = await _context.GenericLootPools
            .FirstOrDefaultAsync(e => e.Status == "Active", cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IGenericLootPool MapToDomain(GenericLootPoolEntity entity)
    {
        var entries = JsonSerializer.Deserialize<List<LootEntry>>(entity.EntriesJson, JsonOptions) ?? [];

        return GenericLootPool.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            entries,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
