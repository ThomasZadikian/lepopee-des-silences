using Leds.Catalog.Application.Rooms.Dtos;
using Leds.Catalog.Application.Rooms.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRoomDefinitionReadStore : IRoomDefinitionReadStore
{
    private readonly CatalogDbContext _context;
    public EfRoomDefinitionReadStore(CatalogDbContext context) => _context = context;

    public async Task<IReadOnlyCollection<RoomDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RoomDefinitions.Where(e => e.Status == "Active").ToListAsync(cancellationToken);
        return entities.Select(e => new RoomDefinitionDto(
            e.Id, e.Key, e.DisplayName, e.Description, e.NarrativeText,
            e.RoomFamily, e.RoomRarity, e.Theme, e.MinDepth, e.MaxDepth, e.BaseWeight, e.SelectionGroup,
            e.EnemyPoolKey, e.RewardPoolKey, e.LawPoolKey, e.CursePoolKey,
            e.SpecialMechanicKey, e.BossDefinitionKey, e.IsUnique, e.IsCulturalEcho, e.Version, e.Status)).ToArray();
    }
}