using Leds.Catalog.Application.RoomThemeAffinities.Dtos;
using Leds.Catalog.Application.RoomThemeAffinities.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfRoomThemeAffinityReadStore : IRoomThemeAffinityReadStore
{
    private readonly CatalogDbContext _context;
    public EfRoomThemeAffinityReadStore(CatalogDbContext context) => _context = context;

    public async Task<IReadOnlyCollection<RoomThemeAffinityDto>> ListAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.RoomThemeAffinities.ToListAsync(cancellationToken);
        return entities.Select(e => new RoomThemeAffinityDto(e.Id, e.ThemeFrom, e.ThemeTo, e.Weight)).ToArray();
    }
}
