using Leds.Catalog.Application.EffectSets.Dtos;
using Leds.Catalog.Application.EffectSets.Ports;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfEffectSetReadStore : IEffectSetReadStore
{
    private readonly CatalogDbContext _context;

    public EfEffectSetReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<EffectSetDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.EffectSets
            .Include(effectSet => effectSet.Effects)
            .FirstOrDefaultAsync(effectSet => effectSet.Key == key, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    private static EffectSetDto MapToDto(EffectSetEntity entity)
    {
        return new EffectSetDto(
            entity.Id,
            entity.Key,
            entity.Version,
            entity.Status,
            entity.Effects
                .OrderBy(effect => effect.Order)
                .Select(MapEffectToDto)
                .ToArray());
    }

    private static EffectDefinitionDto MapEffectToDto(EffectDefinitionEntity entity)
    {
        return new EffectDefinitionDto(
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
