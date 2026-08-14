using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// Pure evaluation of a <see cref="LocalRule"/> against one <see cref="LocalRuleTriggerContext"/>
/// occurrence, advancing <see cref="LocalRuleState"/> as a side effect. Deliberately stops short
/// of APPLYING a consequence (moving an NPC, closing a path, starting combat) — that orchestration
/// belongs to the Application layer, the same split already used for
/// <see cref="Npcs.NpcRelationship"/>'s FractureEvaluator (computes) vs NpcConsequenceApplier
/// (applies).
/// </summary>
public static class LocalRuleEvaluator
{
    public static LocalRuleEvaluationResult Evaluate(
        LocalRule rule, LocalRuleState state, LocalRuleTriggerContext context)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(state);

        if (state.LocalRuleKey != rule.Key)
        {
            throw new DomainException("Local rule state does not belong to this local rule.");
        }

        if (!ConditionIsMet(rule, context))
        {
            return LocalRuleEvaluationResult.NotApplicable();
        }

        if (!state.HasBeenInformed)
        {
            state.MarkInformed();
            return LocalRuleEvaluationResult.Informed(rule.InfoMessage);
        }

        var isFirstTransgression = state.CumulativeSeverity == 0;
        var newConsequences = state.RegisterTransgression(rule);

        return LocalRuleEvaluationResult.Transgression(
            isFirstTransgression ? rule.WarningMessage : null, newConsequences);
    }

    private static bool ConditionIsMet(LocalRule rule, LocalRuleTriggerContext context)
    {
        return rule.ConditionType switch
        {
            LocalRuleConditionType.ZoneEntry => context.PartyX is { } x && context.PartyY is { } y
                && rule.ConditionCells.Contains((x, y)),
            LocalRuleConditionType.ObjectInteraction or LocalRuleConditionType.NpcInteraction =>
                context.InteractedTargetKey is not null
                && context.InteractedTargetKey == rule.ConditionTargetKey,
            _ => false,
        };
    }
}
