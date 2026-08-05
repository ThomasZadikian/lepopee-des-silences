using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Typing;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListActiveSkillDefinitionsQueryHandler
    : IRequestHandler<ListActiveSkillDefinitionsQuery, ListActiveSkillDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListActiveSkillDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListActiveSkillDefinitionsResponse> Handle(
        ListActiveSkillDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitionsTask = _catalogGateway.ListActiveSkillDefinitionsAsync(cancellationToken);
        var npcsTask = _catalogGateway.ListNpcDefinitionsAsync(cancellationToken);
        var itemsTask = _catalogGateway.ListActiveItemDefinitionsAsync(cancellationToken);

        await Task.WhenAll(definitionsTask, npcsTask, itemsTask);

        var definitions = definitionsTask.Result;
        var npcs = npcsTask.Result;
        var items = itemsTask.Result;

        var acquisitionHints = BuildAcquisitionHints(npcs, items);

        return new ListActiveSkillDefinitionsResponse(
            definitions
                // Enemy-exclusive skills never surface in the player-facing Grimoire —
                // enemy AI kits keep resolving skills directly by key regardless (see
                // EnemyDefinition.SkillKeys / CombatEncounterDraftGenerator), unaffected
                // by this filter.
                .Where(d => !string.Equals(d.Audience, "Enemy", StringComparison.OrdinalIgnoreCase))
                .Select(d => new SkillDefinitionView(
                d.Key,
                d.DisplayName,
                d.Description,
                d.SkillType,
                d.TargetingType,
                d.EffectType,
                d.ManaCost,
                d.ChargeCost,
                d.BasePower,
                d.Category,
                d.BasePowerIsPercentOfMaxVitality,
                (d.Effects ?? []).Select(e => new SkillEffectView(
                    e.Kind,
                    e.StatusKey,
                    e.Magnitude,
                    e.DurationTicks,
                    e.TickInterval,
                    e.Stat,
                    e.MagnitudeIsPercentOfMax,
                    e.MagnitudeIsPercentOfBaseStat,
                    e.AppliesToActor,
                    e.IsPermanent)).ToArray(),
                acquisitionHints.TryGetValue(d.Key, out var hints) ? hints : [],
                d.TacticalRange,
                d.TacticalAreaShape,
                d.RequiresLineOfSight,
                d.Cooldown,
                d.IsUltimate,
                d.EmotionalRegister,
                EmotionalTypeProfileProvider.TryResolveIntrinsicType(d.Key, d.Tags, d.EmotionalRegister, out var emotionalType)
                    ? emotionalType.ToString()
                    : null,
                d.AllowedArchetypes,
                d.Audience)).ToArray());
    }

    /// <summary>
    /// Folds NPC offerings (direct "Skill" offerings + companion starting kits) and
    /// item-granted skills into a per-skill-key list of human-readable acquisition
    /// hints, so the Grimoire can tell the player how to unlock a locked skill instead
    /// of a generic placeholder. Mirrors the gateway-composition pattern already used
    /// by NpcEventContentResolutionStrategy (ListNpcDefinitionsAsync + fold).
    /// </summary>
    private static Dictionary<string, List<string>> BuildAcquisitionHints(
        IReadOnlyCollection<CatalogNpcDefinition> npcs,
        IReadOnlyCollection<CatalogItemDefinitionSnapshot> items)
    {
        var hints = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        void AddHint(string? skillKey, string hint)
        {
            if (string.IsNullOrWhiteSpace(skillKey))
                return;

            if (!hints.TryGetValue(skillKey, out var list))
            {
                list = [];
                hints[skillKey] = list;
            }

            list.Add(hint);
        }

        foreach (var npc in npcs)
        {
            foreach (var offering in npc.Offerings ?? [])
            {
                if (string.Equals(offering.Kind, "Skill", StringComparison.OrdinalIgnoreCase))
                {
                    AddHint(offering.TargetKey, $"Offert par {npc.DisplayName}");
                }
                else if (string.Equals(offering.Kind, "Companion", StringComparison.OrdinalIgnoreCase)
                    && offering.CompanionKit is not null)
                {
                    foreach (var skillKey in offering.CompanionKit.SkillKeys)
                    {
                        AddHint(skillKey, $"Sort de départ du compagnon {npc.DisplayName}");
                    }
                }
            }
        }

        foreach (var item in items)
        {
            foreach (var effect in item.EquipmentEffects ?? [])
            {
                if (string.Equals(effect.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase))
                {
                    AddHint(effect.SkillKey, $"Octroyé par l'objet {item.DisplayName}");
                }
            }
        }

        return hints;
    }
}
