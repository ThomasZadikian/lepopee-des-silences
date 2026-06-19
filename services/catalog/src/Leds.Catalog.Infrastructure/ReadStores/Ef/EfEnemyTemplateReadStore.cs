using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfEnemyTemplateReadStore : IEnemyTemplateReadStore
{
    private readonly CatalogDbContext _context;

    public EfEnemyTemplateReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IEnemyTemplate>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.EnemyTemplates
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    public async Task<IEnemyTemplate?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _context.EnemyTemplates
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    private static IEnemyTemplate MapToDomain(EnemyTemplateEntity entity)
    {
        return EnemyTemplate.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            Enum.Parse<EnemyArchetype>(entity.Archetype),
            Enum.Parse<CombatElement>(entity.Element),
            entity.MaxHealth,
            entity.Strength,
            entity.Intelligence,
            entity.Speed,
            entity.PhysicalResistance,
            entity.MagicalResistance,
            entity.ExperienceReward,
            entity.GoldReward,
            Enum.Parse<CatalogContentStatus>(entity.Status));
    }
}
