using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Curses;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfCurseDefinitionReadStore : ICurseDefinitionReadStore, ICurseDefinitionDtoReadStore
{
    private readonly CatalogDbContext _context;

    public EfCurseDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ICurseDefinition>> ListAvailableAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.CurseDefinitions
            .Include(entity => entity.EffectSet)
            .Where(entity => entity.Status == "Active")
            .OrderBy(entity => entity.Key)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<ICurseDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.CurseDefinitions
            .Include(definition => definition.EffectSet)
            .FirstOrDefaultAsync(definition => definition.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<CurseDefinitionDto>> ListAvailableDtosAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.CurseDefinitions
            .Include(entity => entity.EffectSet)
            .Where(entity => entity.Status == "Active")
            .OrderBy(entity => entity.Key)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToArray();
    }

    public async Task<CurseDefinitionDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.CurseDefinitions
            .Include(definition => definition.EffectSet)
            .FirstOrDefaultAsync(definition => definition.Key == key, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    private static ICurseDefinition MapToDomain(CurseDefinitionEntity entity)
    {
        return CurseDefinition.Create(
            entity.Key,
            entity.DisplayName,
            entity.Description,
            entity.Version,
            entity.NarrativeText,
            entity.Severity,
            entity.Duration,
            entity.Trigger,
            entity.EffectSet?.Key,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }

    private static CurseDefinitionDto MapToDto(CurseDefinitionEntity entity)
    {
        return new CurseDefinitionDto(
            entity.Id,
            entity.Key,
            entity.Version,
            entity.DisplayName,
            entity.Description,
            entity.NarrativeText,
            entity.Severity,
            entity.Duration,
            entity.Trigger,
            entity.EffectSet?.Key,
            entity.Status);
    }
}
