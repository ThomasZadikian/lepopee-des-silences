using Leds.Catalog.Application.NpcReputationAffinities.Dtos;
using Leds.Catalog.Application.NpcReputationAffinities.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfNpcReputationAffinityReadStore : INpcReputationAffinityReadStore
{
    private readonly CatalogDbContext _context;
    public EfNpcReputationAffinityReadStore(CatalogDbContext context) => _context = context;

    public async Task<IReadOnlyCollection<NpcReputationAffinityDto>> ListAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.NpcReputationAffinities.ToListAsync(cancellationToken);
        return entities.Select(e => new NpcReputationAffinityDto(e.Id, e.NpcKeyFrom, e.NpcKeyTo, e.Weight)).ToArray();
    }
}
