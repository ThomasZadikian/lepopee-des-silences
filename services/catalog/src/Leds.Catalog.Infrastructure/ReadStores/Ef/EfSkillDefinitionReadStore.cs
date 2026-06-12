using System.Text.Json;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Skills.Definitions;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfSkillDefinitionReadStore : ISkillDefinitionReadStore
{
    private readonly CatalogDbContext _context;

    public EfSkillDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ISkillDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.SkillDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<ISkillDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.SkillDefinitions
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyCollection<ISkillDefinition>> ListByTypeAsync(string skillType, CancellationToken cancellationToken)
    {
        var entities = await _context.SkillDefinitions
            .Where(e => e.Status == "Active" && e.SkillType == skillType)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<ISkillDefinition>> ListByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var entities = await _context.SkillDefinitions
            .Where(e => keys.Contains(e.Key))
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    private static ISkillDefinition MapToDomain(SkillDefinitionEntity entity)
    {
        return SkillDefinition.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            entity.SkillType,
            entity.TargetingType,
            entity.EffectType,
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
