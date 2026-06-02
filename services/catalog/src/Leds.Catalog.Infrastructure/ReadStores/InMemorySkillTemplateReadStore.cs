using Leds.Catalog.Application.Skills.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemorySkillTemplateReadStore : ISkillTemplateReadStore
{
    private readonly IReadOnlyCollection<ISkillTemplate> _templates;

    public InMemorySkillTemplateReadStore()
    {
        _templates =
        [
            SkillTemplate.Create(
                "skill-shadow-bite",
                "Morsure d’Ombre",
                "Une attaque de rupture silencieuse.",
                "catalog-0.1.0",
                CombatElement.Shadow,
                SkillEffectType.Damage,
                SkillTargetType.SingleEnemy,
                manaCost: 3,
                chargeCost: 1,
                basePower: 35,
                healPower: 0,
                status: CatalogContentStatus.Active),

            SkillTemplate.Create(
                "skill-memory-mend",
                "Suture de Mémoire",
                "Restaure une partie de soi.",
                "catalog-0.1.0",
                CombatElement.Memory,
                SkillEffectType.Heal,
                SkillTargetType.SingleAlly,
                manaCost: 4,
                chargeCost: 1,
                basePower: 0,
                healPower: 25,
                status: CatalogContentStatus.Active)
        ];
    }

    public Task<IReadOnlyCollection<ISkillTemplate>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var templates = _templates
            .Where(template => template.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ISkillTemplate>>(templates);
    }

    public Task<ISkillTemplate?> GetByKeyAsync(
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