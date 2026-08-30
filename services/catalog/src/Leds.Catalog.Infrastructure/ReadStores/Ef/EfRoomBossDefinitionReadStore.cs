using System.Text.Json;
using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.RoomBosses;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRoomBossDefinitionReadStore : IRoomBossDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfRoomBossDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IRoomBossDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RoomBossDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IRoomBossDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.RoomBossDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IRoomBossDefinition?> GetByRoomTypeAsync(string roomType, CancellationToken cancellationToken)
    {
        var entity = await _context.RoomBossDefinitions
            .FirstOrDefaultAsync(e => e.RoomType == roomType, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IRoomBossDefinition MapToDomain(RoomBossDefinitionEntity entity)
    {
        var tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson ?? "[]") ?? [];

        return RoomBossDefinition.Create(
            entity.Key,
            entity.DisplayName,
            entity.Description,
            entity.Version,
            entity.RoomType,
            entity.BaseDifficulty,
            tags,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
