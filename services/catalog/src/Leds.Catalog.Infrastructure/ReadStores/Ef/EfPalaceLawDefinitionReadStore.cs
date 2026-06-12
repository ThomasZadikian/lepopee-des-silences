using System.Text.Json;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfPalaceLawDefinitionReadStore : IPalaceLawDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfPalaceLawDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IPalaceLawDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.PalaceLawDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IPalaceLawDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.PalaceLawDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IPalaceLawDefinition MapToDomain(PalaceLawDefinitionEntity entity)
    {
        var impactDomains = JsonSerializer.Deserialize<List<string>>(entity.ImpactDomainsJson) ?? [];

        return PalaceLawDefinition.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<PalaceLawVisibility>(entity.Visibility),
            entity.Priority,
            impactDomains.Select(d => Enum.Parse<PalaceLawImpactDomain>(d)).ToList(),
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
