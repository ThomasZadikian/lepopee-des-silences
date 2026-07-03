using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

/// <summary>
/// Small, bounded heuristic letting certain enemy archetypes hold briefly after
/// becoming ATB-ready to bank a little charge overflow (see
/// AtbActionMath.ChargeDamageMultiplier) before acting — mirrors the boost
/// mechanic players get from Combat.HoldTick. Deliberately stateless:
/// eligibility and the target overflow are both pure, deterministic functions
/// of (combat id, enemy id), so no new persisted field/migration is needed.
/// The natural HoldTick gauge cap (ReadyThreshold + MaxChargeOverflow) already
/// bounds how long an enemy can hold, so this can never stall
/// HoldCombatTurnCommandHandler.MaxEnemyTurnsPerHold or loop indefinitely.
/// </summary>
public static class EnemyBoostHeuristic
{
    /// <summary>Archetypes allowed to hold for a boost — the offense-leaning ones (see UtilityEnemyActionPlanner.ArchetypeWeights).</summary>
    private static readonly HashSet<string> EligibleArchetypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rupture", "elite", "boss",
    };

    /// <summary>Chance (per-mille) that an eligible enemy decides to hold at all, rolled once per enemy per combat.</summary>
    private const int HoldChancePerMille = 400; // 40%

    /// <summary>Bounds (per-mille of MaxChargeOverflow) of the overflow this enemy aims to bank before acting.</summary>
    private const int MinTargetOverflowPerMille = 300; // 30%
    private const int MaxTargetOverflowPerMille = 700;  // 70%

    /// <summary>True if this ready enemy should keep charging instead of acting this hold-tick.</summary>
    public static bool ShouldHoldForMoreCharge(Combatant enemy, Guid combatId)
    {
        if (!EligibleArchetypes.Contains(enemy.Archetype ?? string.Empty))
            return false;

        var picksToHold = DeterministicCombatRoll.UnitInterval($"enemy-ai-boost:{combatId}:{enemy.Id.Value}");
        if (picksToHold * 1000 >= HoldChancePerMille)
            return false; // this enemy never holds this fight

        var target = TargetOverflow(enemy, combatId);
        var overflow = Math.Clamp(enemy.AtbGauge - AtbConstants.ReadyThreshold, 0, AtbConstants.MaxChargeOverflow);
        return overflow < target;
    }

    private static int TargetOverflow(Combatant enemy, Guid combatId)
    {
        var roll = DeterministicCombatRoll.UnitInterval($"enemy-ai-boost-target:{combatId}:{enemy.Id.Value}");
        var perMille = MinTargetOverflowPerMille + (int)(roll * (MaxTargetOverflowPerMille - MinTargetOverflowPerMille));
        return AtbConstants.MaxChargeOverflow * perMille / 1000;
    }
}
