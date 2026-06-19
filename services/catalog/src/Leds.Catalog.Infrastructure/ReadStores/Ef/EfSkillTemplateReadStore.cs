using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Skills;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfSkillTemplateReadStore : ISkillTemplateReadStore
{
    private readonly CatalogDbContext _context;

    public EfSkillTemplateReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ISkillTemplate>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.SkillTemplates
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<ISkillTemplate?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.SkillTemplates
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static ISkillTemplate MapToDomain(SkillTemplateEntity entity)
    {
        return SkillTemplate.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<CombatElement>(entity.Element),
            Enum.Parse<SkillEffectType>(entity.EffectType),
            Enum.Parse<SkillTargetType>(entity.TargetType),
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            entity.HealPower,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
