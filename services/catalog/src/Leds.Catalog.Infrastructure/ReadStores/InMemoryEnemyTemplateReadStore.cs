using Leds.Catalog.Application.Enemies.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryEnemyTemplateReadStore : IEnemyTemplateReadStore
{
    private readonly IReadOnlyCollection<IEnemyTemplate> _templates;

    public InMemoryEnemyTemplateReadStore()
    {
        _templates =
        [
            EnemyTemplate.Create(
                "enemy-shadow-wolf",
                "Loup d’Ombre",
                "Une entité née dans un couloir du Palais.",
                "catalog-0.1.0",
                EnemyArchetype.Trauma,
                CombatElement.Shadow,
                maxHealth: 120,
                strength: 18,
                intelligence: 8,
                speed: 14,
                physicalResistance: 10,
                magicalResistance: 5,
                experienceReward: 25,
                goldReward: 12,
                status: CatalogContentStatus.Active)
        ];
    }

    public Task<IReadOnlyCollection<IEnemyTemplate>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var templates = _templates
            .Where(template => template.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IEnemyTemplate>>(templates);
    }

    public Task<IEnemyTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var template = _templates.SingleOrDefault(template =>
            string.Equals(
                template.Key.Value,
                key,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(template);
    }
}