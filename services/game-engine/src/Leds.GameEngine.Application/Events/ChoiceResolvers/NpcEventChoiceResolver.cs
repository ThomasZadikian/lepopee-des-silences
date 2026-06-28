using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChoiceResolvers;

/// <summary>
/// Resolves a player's choice in an NPC encounter against the NPC's authored dialogue
/// graph: applies the chosen choice's consequences (state-conditioned by the NPC's
/// fracture), evaluates transgressions, refreshes wound states, advances the graph,
/// and performs deterministic reward/curse rolls. Unique combat is deferred to a
/// dedicated wave (needs a catalog Encounter type + the combat-start pipeline).
/// </summary>
public sealed class NpcEventChoiceResolver : ICurrentEventChoiceResolver
{
    private readonly ICatalogContentGateway _catalogContentGateway;

    public NpcEventChoiceResolver(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public NodeEventType EventType => NodeEventType.Npc;

    public async Task<CurrentEventChoiceResolutionResult> ResolveAsync(
        CurrentEventChoiceResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var run = context.Run;
        var npcKey = run.ActiveNpcKey;

        if (string.IsNullOrWhiteSpace(npcKey))
        {
            return Fade(context.ChoiceId);
        }

        var npcs = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npc = npcs.FirstOrDefault(n => string.Equals(n.Key, npcKey, StringComparison.OrdinalIgnoreCase));

        if (npc?.DialogueGraph is null)
        {
            run.EndNpcEncounter();
            return Fade(context.ChoiceId);
        }

        var graph = npc.DialogueGraph;
        var relationship = run.GetNpcRelationship(npcKey) ?? run.BeginOrResumeNpcEncounter(npcKey);

        var currentNodeKey = relationship.CurrentDialogueNodeKey ?? graph.EntryNodeKey;
        if (!graph.Nodes.TryGetValue(currentNodeKey, out var node))
        {
            run.EndNpcEncounter();
            return Fade(context.ChoiceId);
        }

        var choice = node.Choices.FirstOrDefault(c =>
            string.Equals(c.Key, context.ChoiceId, StringComparison.OrdinalIgnoreCase));

        if (choice is null)
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: false,
                $"Choice '{context.ChoiceId}' is not available right now.",
                encounterCompleted: false);
        }

        if (!RequirementsMet(choice.Requirements, relationship))
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: false,
                "This path is closed to you.",
                encounterCompleted: false);
        }

        var fragments = new List<NarrativeFragmentDto>();

        // State captured BEFORE applying consequences — conditions the flip
        // ("drink the water": Latent → reward, Rompu → poison).
        var conditioningState = relationship.AggregateState;

        var applicable = choice.Consequences
            .Where(c => c.WhenWoundState is null ||
                        string.Equals(c.WhenWoundState, conditioningState.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Reward/curse pools are only fetched when a roll is actually due.
        IReadOnlyCollection<CatalogRewardCursePool> pools =
            applicable.Any(c => c.Kind == "RewardOrCurseRoll")
                ? await _catalogContentGateway.ListRewardCursePoolsAsync(cancellationToken)
                : [];

        foreach (var consequence in applicable)
        {
            ApplyConsequence(consequence, npc, run, context.Node, relationship, pools, fragments);
        }

        EvaluateTransgressions(npc, relationship);
        RefreshWounds(npc, relationship);

        relationship.AdvanceTo(choice.NextNodeKey);
        var encounterCompleted = string.IsNullOrWhiteSpace(choice.NextNodeKey);
        if (encounterCompleted)
        {
            run.EndNpcEncounter();
        }

        return CurrentEventChoiceResolutionResult.Create(
            context.ChoiceId,
            accepted: true,
            encounterCompleted ? "La rencontre se referme." : "La conversation se poursuit.",
            fragments,
            encounterCompleted);
    }

    private void ApplyConsequence(
        CatalogDialogueConsequence consequence,
        CatalogNpcDefinition npc,
        Run run,
        MapNode node,
        NpcRelationship relationship,
        IReadOnlyCollection<CatalogRewardCursePool> pools,
        List<NarrativeFragmentDto> fragments)
    {
        switch (consequence.Kind)
        {
            case "Narrative":
                if (!string.IsNullOrWhiteSpace(consequence.NarrativeFragmentKey))
                {
                    // L1: the fragment key doubles as display text until a narrative store exists.
                    fragments.Add(new NarrativeFragmentDto(npc.DisplayName, consequence.NarrativeFragmentKey));
                }
                break;

            case "AdjustRelationship":
                relationship.AdjustScore(consequence.RelationshipDelta);
                break;

            case "SetMemoryFlag":
                if (!string.IsNullOrWhiteSpace(consequence.MemoryFlag))
                {
                    relationship.SetFlag(consequence.MemoryFlag);
                }
                break;

            case "ArmWound":
                if (!string.IsNullOrWhiteSpace(consequence.WoundKey))
                {
                    relationship.SetWoundState(consequence.WoundKey, WoundState.Rompu, canRevert: false);
                }
                break;

            case "SootheWound":
                if (!string.IsNullOrWhiteSpace(consequence.WoundKey))
                {
                    var wound = npc.Wounds?.FirstOrDefault(w =>
                        string.Equals(w.Key, consequence.WoundKey, StringComparison.OrdinalIgnoreCase));
                    var canRevert = wound is not null &&
                        !string.Equals(wound.Reversibility, "Irreversible", StringComparison.OrdinalIgnoreCase);
                    relationship.SetWoundState(consequence.WoundKey, WoundState.Latent, canRevert);
                }
                break;

            case "RewardOrCurseRoll":
                fragments.Add(RollRewardOrCurse(consequence, npc, run, node, relationship, pools));
                break;

            case "TriggerUniqueCombat":
                // Deferred: needs a catalog Encounter type + combat-start pipeline.
                fragments.Add(new NarrativeFragmentDto(npc.DisplayName, "L'air se tend — une confrontation s'ouvre."));
                break;

            default:
                // PalaceAttitudeShift / MarkovShift / EnqueueEvent are L2/L3. Ignored in L1.
                break;
        }
    }

    // ── Reward / curse roll (deterministic) ──────────────────────────────────

    private static NarrativeFragmentDto RollRewardOrCurse(
        CatalogDialogueConsequence consequence,
        CatalogNpcDefinition npc,
        Run run,
        MapNode node,
        NpcRelationship relationship,
        IReadOnlyCollection<CatalogRewardCursePool> pools)
    {
        var poolKey = consequence.RewardCursePoolKey;
        var pool = poolKey is null
            ? null
            : pools.FirstOrDefault(p => string.Equals(p.Key, poolKey, StringComparison.OrdinalIgnoreCase));

        if (pool is null || pool.Entries.Count == 0)
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        // 1) Contextual pool filtering (decision Q16a).
        var eligible = pool.Entries.Where(e => IsAvailable(e, run, node)).ToArray();
        if (eligible.Length == 0)
        {
            return new NarrativeFragmentDto(npc.DisplayName, "Rien ne se produit.");
        }

        // 2) Equiprobable deterministic draw (decision Q5b), seeded & replayable.
        var seed = string.Join('|',
            run.Seed,
            "npc-reward-curse",
            relationship.NpcKey,
            poolKey,
            consequence.WhenWoundState ?? "any",
            relationship.TimesMet.ToString());
        var roll = DeterministicCombatRoll.UnitInterval(seed);
        var index = Math.Min(eligible.Length - 1, (int)(roll * eligible.Length));
        var entry = eligible[index];

        // 3) Apply the drawn effect (L1: Heal applied; others surfaced narratively).
        ApplyRewardCurseEffect(entry, run);

        var label = string.Equals(entry.Kind, "Curse", StringComparison.OrdinalIgnoreCase)
            ? "Une ombre s'installe"
            : "Une faveur t'effleure";
        var amount = entry.Amount != 0 ? $" {entry.Amount}" : string.Empty;
        return new NarrativeFragmentDto(npc.DisplayName, $"{label}. ({entry.ResultKind}{amount})");
    }

    private static bool IsAvailable(CatalogRewardCurseEntry entry, Run run, MapNode node)
    {
        if (entry.Availability is null || entry.Availability.Count == 0)
        {
            return true;
        }

        var vitalityRatio = run.MaxHp > 0 ? (int)(100L * run.CurrentHp / run.MaxHp) : 0;

        foreach (var gate in entry.Availability)
        {
            var ok = gate.Kind switch
            {
                "MinVitalityRatioPercent" => vitalityRatio >= gate.Value,
                "MaxVitalityRatioPercent" => vitalityRatio <= gate.Value,
                "MinActiveLawCount" => run.ActivePalaceLaws.Count >= gate.Value,
                "MinNodeDepth" => node.Row >= gate.Value,
                _ => true
            };

            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyRewardCurseEffect(CatalogRewardCurseEntry entry, Run run)
    {
        switch (entry.ResultKind)
        {
            case "Heal":
                if (entry.Amount > 0)
                {
                    run.ApplyHeal(entry.Amount);
                }
                break;

            // L1: Damage / GrantCurse / GrantLaw / Guard / Mana require run-damage and
            // curse/law construction subsystems — applied in a dedicated follow-on wave.
            default:
                break;
        }
    }

    // ── Fracture / requirements ──────────────────────────────────────────────

    private static bool RequirementsMet(
        IReadOnlyCollection<CatalogDialogueRequirement> requirements,
        NpcRelationship relationship)
    {
        foreach (var requirement in requirements)
        {
            switch (requirement.Kind)
            {
                case "FlagPresent":
                    if (requirement.FlagKey is null || !relationship.HasFlag(requirement.FlagKey)) return false;
                    break;

                case "FlagAbsent":
                    if (requirement.FlagKey is not null && relationship.HasFlag(requirement.FlagKey)) return false;
                    break;

                case "WoundStateAtLeast":
                    if (requirement.WoundKey is null ||
                        !Enum.TryParse<WoundState>(requirement.RequiredWoundState, ignoreCase: true, out var required) ||
                        relationship.GetWoundState(requirement.WoundKey) < required)
                    {
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    private static void EvaluateTransgressions(CatalogNpcDefinition npc, NpcRelationship relationship)
    {
        if (npc.Wounds is null)
        {
            return;
        }

        foreach (var wound in npc.Wounds)
        {
            foreach (var transgression in wound.Transgressions)
            {
                var marker = $"__armed:{wound.Key}:{transgression.TriggerFlag}";
                if (relationship.HasFlag(transgression.TriggerFlag) && !relationship.HasFlag(marker))
                {
                    relationship.AdjustScore(transgression.RelationshipPenalty);
                    relationship.SetWoundState(wound.Key, WoundState.Rompu, canRevert: false);
                    relationship.SetFlag(marker);
                }
            }
        }
    }

    private static void RefreshWounds(CatalogNpcDefinition npc, NpcRelationship relationship)
    {
        if (npc.Wounds is null)
        {
            return;
        }

        foreach (var wound in npc.Wounds)
        {
            var canRevert = string.Equals(wound.Reversibility, "SoothableByScore", StringComparison.OrdinalIgnoreCase);
            relationship.RefreshFromScore(wound.Key, wound.TenseThreshold, wound.RuptureThreshold, canRevert);
        }
    }

    private static CurrentEventChoiceResolutionResult Fade(string choiceId)
    {
        return CurrentEventChoiceResolutionResult.Create(
            choiceId,
            accepted: true,
            "La figure s'efface.",
            new[] { new NarrativeFragmentDto("Elise", "Certaines présences ne font que passer.") },
            encounterCompleted: true);
    }
}