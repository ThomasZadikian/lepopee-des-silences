using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Application.PalaceLaws.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.PalaceLaws;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryPalaceLawDefinitionReadStore : IPalaceLawDefinitionReadStore, IPalaceLawDefinitionDtoReadStore
{
    private readonly IReadOnlyCollection<IPalaceLawDefinition> _definitions;
    private readonly IReadOnlyCollection<PalaceLawDefinitionDto> _dtoDefinitions;

    public InMemoryPalaceLawDefinitionReadStore()
    {
        _definitions =
        [
            CreateSilenceLaw(),
            CreateRuptureLaw()
        ];

        _dtoDefinitions = CreateDtoDefinitions();
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

    public Task<IReadOnlyCollection<PalaceLawDefinitionDto>> ListActiveDtosAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _dtoDefinitions
            .Where(definition => string.Equals(definition.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(definition => definition.Priority)
            .ThenBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<PalaceLawDefinitionDto>>(definitions);
    }

    public Task<PalaceLawDefinitionDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = _dtoDefinitions.SingleOrDefault(definition =>
            string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    private static IReadOnlyCollection<PalaceLawDefinitionDto> CreateDtoDefinitions()
    {
        return
        [
            CreateDto("law-silence-v1", "Loi du Silence", "Le silence deforme la generation.", 10, ["Generation", "Narrative"], "effect.law.silence-weight", [Effect("ModifyGenerationWeight", "SelectionContext", 0.10m, "UntilRunEnds", generationTag: "generation.silence")]),
            CreateDto("law-rupture-v1", "Loi de Rupture", "La rupture augmente le risque des evenements.", 20, ["Events", "Rewards", "Combat"], null, []),
            CreateDto("law-aegis-v1", "Loi de l'Égide", "La premiere garde du heros se renforce.", 30, ["Combat"], "effect.law.aegis", [Effect("AddStartingGuard", "Run", 8m, "UntilRunEnds")]),
            CreateDto("law-siege-v1", "Loi du Siege", "Les prochains affrontements gagnent en pression.", 40, ["Combat"], "effect.law.siege", [Effect("ModifyDifficultyMultiplier", "Run", 0.10m, "UntilRunEnds")]),
            CreateDto("law-carnage-v1", "Loi du Carnage", "La puissance d'attaque du heros augmente.", 50, ["Combat"], "effect.law.carnage", [Effect("ModifyAttackPower", "Run", 0.10m, "UntilRunEnds")]),
            CreateDto("law-tempest-v1", "Loi de la Pluie", "La Room actuelle est traversee par la Pluie.", 60, ["Combat"], "effect.law.climate-rain", [Effect("ApplyRoomClimate", "CurrentRoom", 0m, "UntilRoomEnds", valueMode: "TagOnly", stackPolicy: "UniqueBySource", condition: "Rain")]),
            CreateDto("law-hail-v1", "Loi de la Grele", "La Room actuelle est traversee par la Grele.", 70, ["Combat"], "effect.law.climate-hail", [Effect("ApplyRoomClimate", "CurrentRoom", 0m, "UntilRoomEnds", valueMode: "TagOnly", stackPolicy: "UniqueBySource", condition: "Hail")]),
            CreateDto("law-drought-v1", "Loi de la Canicule", "La Room actuelle est ecrasee par la Canicule.", 80, ["Combat"], "effect.law.climate-heatwave", [Effect("ApplyRoomClimate", "CurrentRoom", 0m, "UntilRoomEnds", valueMode: "TagOnly", stackPolicy: "UniqueBySource", condition: "Heatwave")]),
            CreateDto("law-grey-v1", "Loi de la Grisaille", "La Room actuelle est recouverte de Grisaille.", 90, ["Combat"], "effect.law.climate-grey", [Effect("ApplyRoomClimate", "CurrentRoom", 0m, "UntilRoomEnds", valueMode: "TagOnly", stackPolicy: "UniqueBySource", condition: "Grey")])
        ];
    }

    private static PalaceLawDefinitionDto CreateDto(
        string key,
        string name,
        string description,
        int priority,
        IReadOnlyCollection<string> impactDomains,
        string? effectSetKey,
        IReadOnlyCollection<PalaceLawEffectDefinitionDto> effects)
    {
        return new PalaceLawDefinitionDto(
            Guid.NewGuid(),
            key,
            name,
            description,
            "1.0.0",
            "Active",
            "Visible",
            priority,
            impactDomains,
            effectSetKey,
            effects);
    }

    private static PalaceLawEffectDefinitionDto Effect(
        string effectType,
        string targetScope,
        decimal value,
        string duration,
        string valueMode = "Flat",
        string stackPolicy = "Additive",
        string? condition = null,
        string? behaviorTag = null,
        string? generationTag = null)
    {
        return new PalaceLawEffectDefinitionDto(
            effectType,
            targetScope,
            value,
            valueMode,
            duration,
            stackPolicy,
            condition,
            0,
            behaviorTag,
            generationTag,
            null);
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
