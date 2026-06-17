using System.Text.Json;
using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfPalaceLawDefinitionReadStore : IPalaceLawDefinitionReadStore, IPalaceLawDefinitionDtoReadStore
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

    public async Task<IReadOnlyCollection<PalaceLawDefinitionDto>> ListActiveDtosAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.PalaceLawDefinitions
            .Include(e => e.EffectSet)
            .ThenInclude(e => e!.Effects)
            .Where(e => e.Status == "Active")
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.Key)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToArray();
    }

    public async Task<IPalaceLawDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.PalaceLawDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<PalaceLawDefinitionDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.PalaceLawDefinitions
            .Include(e => e.EffectSet)
            .ThenInclude(e => e!.Effects)
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDto(entity);
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

    private static PalaceLawDefinitionDto MapToDto(PalaceLawDefinitionEntity entity)
    {
        var impactDomains = JsonSerializer.Deserialize<List<string>>(entity.ImpactDomainsJson) ?? [];

        return new PalaceLawDefinitionDto(
            entity.Id,
            entity.Key,
            string.IsNullOrWhiteSpace(entity.DisplayName) ? entity.Name : entity.DisplayName,
            entity.Description,
            entity.Version,
            entity.Status,
            entity.Visibility,
            entity.Priority,
            impactDomains,
            entity.EffectSet?.Key,
            entity.EffectSet?.Effects
                .OrderBy(effect => effect.Order)
                .Select(MapEffectToDto)
                .ToArray() ?? []);
    }

    private static PalaceLawEffectDefinitionDto MapEffectToDto(EffectDefinitionEntity entity)
    {
        return new PalaceLawEffectDefinitionDto(
            entity.EffectType,
            entity.TargetScope,
            entity.Value ?? 0m,
            entity.ValueMode,
            entity.Duration,
            entity.StackPolicy,
            entity.Condition,
            entity.Order,
            entity.BehaviorTag,
            entity.GenerationTag,
            entity.SelectionGroup);
    }
}
