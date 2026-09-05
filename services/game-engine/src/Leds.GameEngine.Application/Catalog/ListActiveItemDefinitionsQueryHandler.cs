using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListActiveItemDefinitionsQueryHandler
    : IRequestHandler<ListActiveItemDefinitionsQuery, ListActiveItemDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListActiveItemDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListActiveItemDefinitionsResponse> Handle(
        ListActiveItemDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListActiveItemDefinitionsAsync(cancellationToken);

        return new ListActiveItemDefinitionsResponse(
            definitions.Select(d => new ItemDefinitionView(
                d.Key,
                d.DisplayName,
                d.Description,
                d.Category,
                d.FlavorTag,
                d.Rarity,
                d.EffectRunType,
                d.EffectValue,
                d.AllowedSlots is { Count: 1 } ? d.AllowedSlots.Single() : null,
                d.ReadablePages,
                (d.EquipmentEffects ?? []).Select(effect => new ItemEquipmentEffectView(
                    effect.Kind,
                    effect.StatKind,
                    effect.Amount,
                    effect.SkillKey,
                    effect.AffinityRegister,
                    effect.Condition,
                    effect.AffinityOutcome,
                    effect.Priority,
                    effect.DurationActivations,
                    effect.BehaviorCode)).ToArray(),
                d.IsContainer,
                d.ContainerCapacity,
                d.IsLiquid,
                d.TacticalRange,
                d.TacticalAreaShape,
                d.RequiresLineOfSight,
                d.BasicAttackPower,
                d.BasicAttackCategory,
                d.PalaceShardCost,
                d.HimLitShardCost,
                d.AllowedSlots ?? [],
                d.UniqueEquipGroup,
                d.ProficiencyTags ?? [])).ToArray());
    }
}
