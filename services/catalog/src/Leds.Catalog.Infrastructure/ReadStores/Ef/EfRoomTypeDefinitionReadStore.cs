using Leds.Catalog.Application.RoomTypes.Dtos;
using Leds.Catalog.Application.RoomTypes.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRoomTypeDefinitionReadStore : IRoomTypeDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfRoomTypeDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RoomTypeDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RoomTypeDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => new RoomTypeDefinitionDto(
                e.Id,
                e.Key,
                e.DisplayName,
                e.Description,
                e.Theme,
                e.DefaultRarity,
                e.MinDepth,
                e.MaxDepth,
                e.Version,
                e.Status))
            .ToArray();
    }
}
