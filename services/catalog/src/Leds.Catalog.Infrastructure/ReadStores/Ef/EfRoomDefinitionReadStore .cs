using Leds.Catalog.Application.Rooms.Dtos;
using Leds.Catalog.Application.Rooms.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRoomDefinitionReadStore : IRoomDefinitionReadStore
{
    private readonly CatalogDbContext _context;
    public EfRoomDefinitionReadStore(CatalogDbContext context) => _context = context;

    public async Task<IReadOnlyCollection<RoomDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RoomDefinitions
            .Include(e => e.WorldDefinition)
            .Include(e => e.ReachableTo)
                .ThenInclude(r => r.ToRoomDefinition)
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(e => new RoomDefinitionDto(
            e.Id, e.Key, e.DisplayName, e.Description, e.NarrativeText,
            e.RoomFamily, e.RoomRarity, e.Theme, e.MinDepth, e.MaxDepth, e.BaseWeight, e.SelectionGroup,
            e.EnemyPoolKey, e.RewardPoolKey, e.LawPoolKey, e.CursePoolKey,
            e.SpecialMechanicKey, e.BossDefinitionKey, e.IsUnique, e.IsCulturalEcho,
            e.WorldDefinition?.Key,
            e.WorldDefinition is not null && e.WorldDefinition.EntryRoomDefinitionId == e.Id,
            e.TriggersStrictChain,
            ResolveReachableKeys(e, entities),
            e.Version, e.Status)).ToArray();
    }

    /// <summary>
    /// For rooms authored with an explicit allow-list, returns the listed target keys as-is.
    /// For rooms authored as a deny-list ("AllExceptListed", e.g. room.couloirs), resolves the
    /// effective allow-list here — every other active room in the same World, minus the listed
    /// exclusions and minus any room marked <see cref="RoomDefinitionEntity.ExcludeFromOpenPool"/>
    /// (strict-chain-only rooms). Game-engine only ever sees a flat resolved list.
    /// </summary>
    private static IReadOnlyCollection<string> ResolveReachableKeys(
        RoomDefinitionEntity room, IReadOnlyCollection<RoomDefinitionEntity> allRooms)
    {
        var listedKeys = room.ReachableTo
            .Select(r => r.ToRoomDefinition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.Equals(room.ReachabilityMode, "AllExceptListed", StringComparison.OrdinalIgnoreCase))
        {
            return listedKeys.ToArray();
        }

        return allRooms
            .Where(other => other.Id != room.Id
                && other.WorldDefinitionId == room.WorldDefinitionId
                && !other.ExcludeFromOpenPool
                && !listedKeys.Contains(other.Key))
            .Select(other => other.Key)
            .ToArray();
    }
}
