using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryPalaceLawDefinitionReadStore : IPalaceLawDefinitionReadStore
{
    private readonly IReadOnlyCollection<IPalaceLawDefinition> _definitions;

    public InMemoryPalaceLawDefinitionReadStore()
    {
        _definitions =
        [
            CreateSilenceLaw(),
            CreateRuptureLaw()
        ];
    }

    public Task<IReadOnlyCollection<IPalaceLawDefinition>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(definition => definition.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IPalaceLawDefinition>>(definitions);
    }

    public Task<IPalaceLawDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(definition =>
            string.Equals(
                definition.Key.Value,
                key,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    private static PalaceLawDefinition CreateSilenceLaw()
    {
        var law = PalaceLawDefinition.Create(
            "law-silence-v1",
            "Loi du Silence",
            "Le silence déforme la génération et modifie les fragments narratifs.",
            "law-1.0.0",
            PalaceLawVisibility.PartiallyVisible,
            priority: 10,
            impactDomains:
            [
                PalaceLawImpactDomain.Generation,
                PalaceLawImpactDomain.Narrative
            ],
            status: CatalogContentStatus.Draft);

        law.Activate();

        return law;
    }

    private static PalaceLawDefinition CreateRuptureLaw()
    {
        var law = PalaceLawDefinition.Create(
            "law-rupture-v1",
            "Loi de Rupture",
            "La rupture augmente le risque des événements et altère les récompenses.",
            "law-1.0.0",
            PalaceLawVisibility.Visible,
            priority: 20,
            impactDomains:
            [
                PalaceLawImpactDomain.Events,
                PalaceLawImpactDomain.Rewards,
                PalaceLawImpactDomain.Combat
            ],
            status: CatalogContentStatus.Draft);

        law.Activate();

        return law;
    }
}