using Leds.Catalog.Application.Worlds.Dtos;
using Leds.Catalog.Application.Worlds.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfWorldDefinitionReadStore : IWorldDefinitionReadStore
{
    private readonly CatalogDbContext _context;
    public EfWorldDefinitionReadStore(CatalogDbContext context) => _context = context;

    public async Task<IReadOnlyCollection<WorldDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.WorldDefinitions
            .Include(e => e.EntryRoomDefinition)
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(e => new WorldDefinitionDto(
            e.Id, e.Key, e.DisplayName, e.EntryRoomDefinition.Key, e.Version, e.Status)).ToArray();
    }
}
