using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Combats.Typing;
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
        var charactersTask = _catalogGateway.ListCharacterCombatDefinitionsAsync(cancellationToken);

        await Task.WhenAll(definitionsTask, npcsTask, itemsTask, charactersTask);

        var definitions = definitionsTask.Result;
        var npcs = npcsTask.Result;
        var items = itemsTask.Result;
        var characters = charactersTask.Result;

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
                EmotionalRegister: EmotionalTypeCode.NormalizeRequired(
                    d.EmotionalRegister,
                    $"Skill '{d.Key}' EmotionalRegister"),
                TacticalRange: d.TacticalRange,
                TacticalAreaShape: d.TacticalAreaShape,
                RequiresLineOfSight: d.RequiresLineOfSight,
                Cooldown: d.Cooldown,
                IsUltimate: d.IsUltimate,
                CompatibleCharacterDefinitionKeys: CompatibleCharacters(d.AllowedArchetypes, characters),
                Audience: d.Audience)).ToArray());
    }

    private static IReadOnlyCollection<string> CompatibleCharacters(
        IReadOnlyCollection<string>? allowedArchetypes,
        IReadOnlyCollection<CatalogCharacterCombatDefinition> characters)
    {
        if (allowedArchetypes is not { Count: > 0 })
            return characters.Select(character => character.DefinitionKey).ToArray();

        return characters
            .Where(character => string.Equals(
                    character.CombatArchetypeCode, "Adaptive", StringComparison.OrdinalIgnoreCase)
                || allowedArchetypes.Contains(
                    character.CombatArchetypeCode, StringComparer.OrdinalIgnoreCase))
            .Select(character => character.DefinitionKey)
            .ToArray();
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
