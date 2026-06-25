using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Markov.Psyche;

namespace Leds.GameEngine.Application.Combats.Atb;

/// <summary>
/// Default ATB tempo provider: FillPerTick = Speed × roomFactor × combatantFactor,
/// both derived from the Palace's dominant emotional state (Markov). Erratic states
/// use deterministic SHA-256 jitter. Opening gauge = Initiative stagger + Markov bias.
/// </summary>
public sealed class MarkovAtbTempoProvider : IAtbTempoProvider
{
    public AtbTempoResult Resolve(AtbTempoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dominant = context.Psyche?.Dominant() ?? EmotionalState.Calm;

        var roomFactor = dominant == EmotionalState.Dissociated
            ? DeterministicAtbJitter.FactorPerMille(
                context.Seed, context.Tick, "room",
                AtbTempoCalibration.JitterMinPerMille, AtbTempoCalibration.JitterMaxPerMille)
            : AtbTempoCalibration.RoomFactorPerMille(dominant);

        var combatantFactor = dominant == EmotionalState.Fragmented
            ? DeterministicAtbJitter.FactorPerMille(
                context.Seed, context.Tick, context.CombatantKey,
                AtbTempoCalibration.JitterMinPerMille, AtbTempoCalibration.JitterMaxPerMille)
            : AtbTempoCalibration.CombatantFactorPerMille(context.Side, dominant);

        var fill = (int)Math.Max(1, (long)context.Speed * roomFactor * combatantFactor / 1_000_000);

        var openingBias = AtbTempoCalibration.OpeningBias(context.Side, dominant);

        // Deterministic per-combatant opening spread so equal-speed combatants don't
        // all reach the threshold on the same tick — readable, staggered ATB bars.
        var openingSpread = DeterministicAtbJitter.FactorPerMille(
            context.Seed, 0, "atb-open:" + context.CombatantKey, 0, AtbConstants.ReadyThreshold / 2);

        var openingGauge = AtbActionMath.InitialGauge(context.Initiative, openingBias + openingSpread);

        return new AtbTempoResult(fill, openingGauge);
    }
}