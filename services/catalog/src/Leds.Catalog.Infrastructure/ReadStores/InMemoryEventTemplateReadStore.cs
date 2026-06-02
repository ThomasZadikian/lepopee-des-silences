using Leds.Catalog.Application.EventTemplates.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.EventTemplates;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryEventTemplateReadStore : IEventTemplateReadStore
{
    private readonly IReadOnlyCollection<IEventTemplate> _templates;

    public InMemoryEventTemplateReadStore()
    {
        _templates =
        [
            CreateMemoryThresholdEvent(),
            CreateLawSilenceEvent(),
            CreateCombatShadowEvent()
        ];
    }

    public Task<IReadOnlyCollection<IEventTemplate>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var templates = _templates
            .Where(template => template.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IEventTemplate>>(templates);
    }

    public Task<IEventTemplate?> GetByKeyAsync(
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

    private static EventTemplate CreateMemoryThresholdEvent()
    {
        var template = EventTemplate.Create(
            "event-memory-threshold-v1",
            "Mémoire du seuil",
            "Une mémoire apparaît à l’entrée du Palais.",
            "event-1.0.0",
            EventTemplateType.Memory,
            EventOutcomeKind.TomePageUnlocked,
            minRiskLevel: 5,
            maxRiskLevel: 20,
            requiresPlayerChoice: true,
            narrativeTags:
            [
                "memory",
                "threshold",
                "elise"
            ],
            status: CatalogContentStatus.Draft);

        template.Activate();

        return template;
    }

    private static EventTemplate CreateLawSilenceEvent()
    {
        var template = EventTemplate.Create(
            "event-law-silence-v1",
            "Écho du Silence",
            "Une Loi du Palais se manifeste dans la pièce.",
            "event-1.0.0",
            EventTemplateType.Law,
            EventOutcomeKind.PalaceLawApplied,
            minRiskLevel: 10,
            maxRiskLevel: 35,
            requiresPlayerChoice: true,
            narrativeTags:
            [
                "law",
                "silence",
                "palace"
            ],
            status: CatalogContentStatus.Draft);

        template.Activate();

        return template;
    }

    private static EventTemplate CreateCombatShadowEvent()
    {
        var template = EventTemplate.Create(
            "event-combat-shadow-v1",
            "Combat d’ombre",
            "Une ombre du Palais bloque le chemin.",
            "event-1.0.0",
            EventTemplateType.Combat,
            EventOutcomeKind.CombatStarted,
            minRiskLevel: 15,
            maxRiskLevel: 45,
            requiresPlayerChoice: false,
            narrativeTags:
            [
                "combat",
                "shadow"
            ],
            status: CatalogContentStatus.Draft);

        template.Activate();

        return template;
    }
}