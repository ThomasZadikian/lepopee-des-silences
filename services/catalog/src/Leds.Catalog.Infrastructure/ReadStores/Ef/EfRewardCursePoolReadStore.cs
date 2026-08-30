using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Application.RewardCursePools.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.RewardCursePools;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRewardCursePoolReadStore : IRewardCursePoolReadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public EfRewardCursePoolReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IRewardCursePool>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RewardCursePools
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    private static IRewardCursePool MapToDomain(RewardCursePoolEntity entity)
    {
        var entries = JsonSerializer.Deserialize<List<RewardCurseEntry>>(entity.EntriesJson, JsonOptions) ?? [];

        return RewardCursePool.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            entries,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}