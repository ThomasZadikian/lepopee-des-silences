using System.Text.Json;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfNpcDefinitionReadStore : INpcDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfNpcDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<INpcDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.NpcDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    private static INpcDefinition MapToDomain(NpcDefinitionEntity entity)
    {
        var tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? [];
        var compatibleRoomTypes = JsonSerializer.Deserialize<List<string>>(entity.CompatibleRoomTypesJson) ?? [];
        var compatiblePalaceRoomStates = JsonSerializer.Deserialize<List<string>>(entity.CompatiblePalaceRoomStatesJson) ?? [];
        var compatibleRoomClimates = JsonSerializer.Deserialize<List<string>>(entity.CompatibleRoomClimatesJson) ?? [];

        return NpcDefinition.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            tags,
            compatibleRoomTypes,
            compatiblePalaceRoomStates,
            compatibleRoomClimates,
            entity.MinDepth,
            entity.MaxDepth,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
