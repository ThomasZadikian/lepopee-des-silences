namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// The result of resolving a single damage instance: the final amount applied
/// plus the breakdown (effectiveness, type multiplier, whether it critically hit).
/// </summary>
public readonly record struct DamageOutcome(
    int FinalAmount,
    int RawAmount,
    DamageEffectiveness Effectiveness,
    double TypeMultiplier,
    bool IsCritical);

/// <summary>
/// Pure, deterministic damage math: base power → type effectiveness → critical
/// hit. Given the same inputs (including the deterministic crit roll), it always
/// produces the same outcome, so combat stays server-authoritative and replayable.
/// </summary>
public static class DamageCalculator
{
    public static DamageOutcome Calculate(
        int basePower,
        EmotionalType attackType,
        CombatantTypeProfile defender,
        double critChance,
        double critRoll,
        int minimumDamage = 1,
        double critMultiplier = CriticalHitCalibration.CritMultiplier)
    {
        if (basePower < 0)
        {
            basePower = 0;
        }

        var effectiveness = defender.EffectivenessAgainst(attackType);
        var typeMultiplier = DamageEffectivenessMultipliers.For(effectiveness);

        // An immune hit can never crit (it deals nothing); otherwise a crit lands
        // when the deterministic roll falls under the attacker's crit chance.
        var isCritical = effectiveness != DamageEffectiveness.Immune
            && critChance > 0
            && critRoll < critChance;

        var appliedCritMultiplier = isCritical ? critMultiplier : 1.0;

        var scaled = basePower * typeMultiplier * appliedCritMultiplier;
        var finalAmount = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);

        if (effectiveness == DamageEffectiveness.Immune)
        {
            // Immunity always zeroes the hit.
            finalAmount = 0;
        }
        else if (basePower > 0 && finalAmount < minimumDamage)
        {
            // A non-immune hit from a damaging skill always lands for at least 1.
            finalAmount = minimumDamage;
        }

        return new DamageOutcome(finalAmount, basePower, effectiveness, typeMultiplier, isCritical);
    }
}
