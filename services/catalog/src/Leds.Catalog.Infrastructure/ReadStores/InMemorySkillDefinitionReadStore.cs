using Leds.Catalog.Application.Skills.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemorySkillDefinitionReadStore : ISkillDefinitionReadStore
{
    private readonly IReadOnlyCollection<ISkillDefinition> _definitions;

    public InMemorySkillDefinitionReadStore()
    {
        _definitions =
        [
            CreateBasicStrike(),
            CreateBasicGuard(),
            CreateBasicWeaken(),
            CreateBasicDisrupt(),
            CreateBasicFocus()
        ];
    }

    public Task<IReadOnlyCollection<ISkillDefinition>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(definitions);
    }

    public Task<ISkillDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(d =>
            string.Equals(d.Key.Value, key, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    public Task<IReadOnlyCollection<ISkillDefinition>> ListByTypeAsync(
        string skillType,
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .Where(d => string.Equals(d.SkillType, skillType, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(definitions);
    }

    public Task<IReadOnlyCollection<ISkillDefinition>> ListByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        if (keys is null || keys.Count == 0)
        {
            return Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(Array.Empty<ISkillDefinition>());
        }

        var definitions = _definitions
            .Where(d => keys.Any(k =>
                string.Equals(d.Key.Value, k, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ISkillDefinition>>(definitions);
    }

    private static SkillDefinition CreateBasicStrike()
    {
        var def = SkillDefinition.Create(
            "skill.basic.strike",
            "Frappe",
            "Une attaque de base qui inflige des degats legers a un ennemi.",
            "1.0.0",
            "Damage",
            "SingleEnemy",
            "Damage",
            manaCost: 5,
            chargeCost: 0,
            basePower: 10,
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static SkillDefinition CreateBasicGuard()
    {
        var def = SkillDefinition.Create(
            "skill.basic.guard",
            "Garde",
            "Une posture defensive qui reduit les degats subis pendant un tour.",
            "1.0.0",
            "Defense",
            "Self",
            "Buff",
            manaCost: 3,
            chargeCost: 1,
            basePower: 0,
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static SkillDefinition CreateBasicWeaken()
    {
        var def = SkillDefinition.Create(
            "skill.basic.weaken",
            "Affaiblissement",
            "Une malédiction qui reduit la puissance d'un ennemi.",
            "1.0.0",
            "Debuff",
            "SingleEnemy",
            "Debuff",
            manaCost: 4,
            chargeCost: 0,
            basePower: 0,
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static SkillDefinition CreateBasicDisrupt()
    {
        var def = SkillDefinition.Create(
            "skill.basic.disrupt",
            "Perturbation",
            "Une interference qui desorganise les competences ennemies.",
            "1.0.0",
            "Debuff",
            "SingleEnemy",
            "Debuff",
            manaCost: 6,
            chargeCost: 1,
            basePower: 0,
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static SkillDefinition CreateBasicFocus()
    {
        var def = SkillDefinition.Create(
            "skill.basic.focus",
            "Concentration",
            "Un etat de focalisation qui augmente la puissance du prochain sort.",
            "1.0.0",
            "Buff",
            "Self",
            "Buff",
            manaCost: 2,
            chargeCost: 0,
            basePower: 0,
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }
}
