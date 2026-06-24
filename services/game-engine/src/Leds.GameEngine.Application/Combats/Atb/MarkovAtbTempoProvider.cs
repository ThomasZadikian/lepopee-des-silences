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
        var openingGauge = AtbActionMath.InitialGauge(context.Initiative, openingBias);

        return new AtbTempoResult(fill, openingGauge);
    }
}