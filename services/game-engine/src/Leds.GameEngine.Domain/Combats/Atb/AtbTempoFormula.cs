namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Pure, deterministic ATB fill-rate formula. Combines a combatant's EFFECTIVE
/// stats (base + any active buff/debuff, equipment %, etc. — never the frozen
/// base snapshot) so tempo reacts live to anything that changes those stats,
/// present or future.
/// </summary>
public static class AtbTempoFormula
{
    // Same baseline idiom as the damage formula (CombatSkillEffectResolver):
    // a combatant sitting exactly at this Attack+Defense total pays no tempo
    // cost or bonus. Investing further above it (offense and/or defense) taxes
    // tempo — a build cannot simultaneously max stats and act most often.
    private const double InvestmentBaseline = 20.0;

    // How much a Speed edge/deficit vs the opposing side's average EFFECTIVE
    // Speed shifts tempo, clamped to ±50% so no single matchup runs away.
    private const double RelativeSpeedSpread = 20.0;
    private const double RelativeSpeedClamp = 0.5;

    /// <param name="effectiveSpeed">Combatant's current EffectiveSpeed.</param>
    /// <param name="effectiveAttackPower">Combatant's current EffectiveAttackPower.</param>
    /// <param name="effectiveDefense">Combatant's current EffectiveDefense.</param>
    /// <param name="opponentAverageEffectiveSpeed">
    /// Average EffectiveSpeed of the living combatants on the OPPOSING side,
    /// recomputed live so debuffing/buffing an opponent's Speed shifts this
    /// combatant's relative standing over the course of the fight.
    /// </param>
    /// <param name="roomFactorPerMille">Markov room factor, baked once at combat prep (1000 = neutral).</param>
    /// <param name="combatantFactorPerMille">Markov per-side factor, baked once at combat prep (1000 = neutral).</param>
    /// <param name="tempoMomentumPerMille">Temporary tempo boost from recent impactful actions (0 = none).</param>
    public static int ComputeFillPerTick(
        int effectiveSpeed,
        int effectiveAttackPower,
        int effectiveDefense,
        double opponentAverageEffectiveSpeed,
        int roomFactorPerMille,
        int combatantFactorPerMille,
        int tempoMomentumPerMille)
    {
        var investment = InvestmentBaseline
            / (InvestmentBaseline + Math.Max(0, effectiveAttackPower + effectiveDefense - 2 * InvestmentBaseline));

        var relative = 1.0 + Math.Clamp(
            (effectiveSpeed - opponentAverageEffectiveSpeed) / RelativeSpeedSpread,
            -RelativeSpeedClamp, RelativeSpeedClamp);

        var momentum = 1.0 + Math.Max(0, tempoMomentumPerMille) / 1000.0;

        var raw = effectiveSpeed
            * investment
            * relative
            * momentum
            * roomFactorPerMille
            * combatantFactorPerMille
            / 1_000_000.0;

        return Math.Max(1, (int)raw);
    }
}
