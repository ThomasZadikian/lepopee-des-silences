using Leds.GameEngine.Domain.Protocol;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Protocol;

/// <summary>One rule's outcome for a single move — surfaced to the caller (API/frontend) even
/// when nothing mechanical was applied, so a consequence like VeilleurApproach or Combat is never
/// silently dropped just because no runtime machinery consumes it yet.</summary>
public sealed record LocalRuleTriggerOutcome(string RuleKey, string RuleName, LocalRuleEvaluationResult Result);

/// <summary>
/// Runtime side of the generic LocalRule engine — <see cref="LocalRuleEvaluator"/> (Ensemble 2)
/// is pure Domain and stops short of resolving the authored <see cref="LocalRule"/> or applying
/// its consequences, by design (see that class's own remarks). This is the first Application-layer
/// caller: it resolves rules via <see cref="ILocalRuleProvider"/>, detects true zone crossings —
/// SFD Hall d'entrée §V's "quitte le tapis puis y revient" is an edge (leaving then re-entering),
/// not a level held while walking cell-to-cell inside an already-entered zone — and applies
/// whichever consequences already have real runtime machinery.
/// <para>
/// Only <see cref="LocalRuleConsequenceType.NpcRelocate"/> and
/// <see cref="LocalRuleConsequenceType.IncreasedSurveillance"/> are applied here, both via
/// <see cref="Npcs.RoomNpc.RaiseAlert"/> exactly as that method's own remarks intend. The rest of
/// the ladder — AttitudeChange (needs Ensemble 3's NpcRelationship, a different aggregate this
/// evaluator has no reach into), AccessClosure (RoomGrid has no mutable door state yet),
/// VeilleurApproach/Combat (no Veilleur spawning or combat-trigger path exists yet) — has no
/// runtime counterpart today. They still come back in <see cref="LocalRuleTriggerOutcome"/> rather
/// than being dropped.
/// </para>
/// </summary>
public sealed class LocalRuleProtocolEvaluator
{
    private readonly ILocalRuleProvider _provider;

    public LocalRuleProtocolEvaluator(ILocalRuleProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Walks <paramref name="traversedCells"/> in order, starting from
    /// (<paramref name="previousX"/>, <paramref name="previousY"/>) — the party's position right
    /// before the move — checking every <see cref="Room.LocalRuleStates"/> entry whose rule is
    /// <see cref="LocalRuleConditionType.ZoneEntry"/> for a genuine crossing into its condition
    /// cells. A rule already satisfied at the pre-move cell never re-fires just because the party
    /// keeps walking through more of its own cells.
    /// </summary>
    public IReadOnlyList<LocalRuleTriggerOutcome> EvaluateZoneCrossings(
        Room room, int previousX, int previousY, IReadOnlyList<(int X, int Y)> traversedCells)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(traversedCells);

        if (room.LocalRuleStates.Count == 0 || traversedCells.Count == 0)
        {
            return [];
        }

        var outcomes = new List<LocalRuleTriggerOutcome>();
        var priorX = previousX;
        var priorY = previousY;

        foreach (var (x, y) in traversedCells)
        {
            foreach (var state in room.LocalRuleStates)
            {
                var rule = _provider.GetByKey(state.LocalRuleKey);
                if (rule is null || rule.ConditionType != LocalRuleConditionType.ZoneEntry)
                {
                    continue;
                }

                var wasInZone = rule.ConditionCells.Contains((priorX, priorY));
                var isInZone = rule.ConditionCells.Contains((x, y));

                if (wasInZone || !isInZone)
                {
                    continue;
                }

                var result = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(x, y));
                if (result.Outcome == LocalRuleEvaluationOutcome.NotApplicable)
                {
                    continue;
                }

                ApplyMechanicalConsequences(room, result.NewConsequences);
                outcomes.Add(new LocalRuleTriggerOutcome(rule.Key, rule.Name, result));
            }

            priorX = x;
            priorY = y;
        }

        return outcomes;
    }

    private static void ApplyMechanicalConsequences(Room room, IReadOnlyList<LocalRuleConsequence> consequences)
    {
        foreach (var consequence in consequences)
        {
            if (consequence.Type is not (LocalRuleConsequenceType.NpcRelocate or LocalRuleConsequenceType.IncreasedSurveillance))
            {
                continue;
            }

            var target = room.RoomNpcs.FirstOrDefault(npc => npc.CatalogNpcKey == consequence.TargetNpcCatalogKey);
            target?.RaiseAlert();
        }
    }
}
