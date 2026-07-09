namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Tuning surface for the ATB "momentum" resource: landing an impactful action
/// (critical hit, breaking an opponent's guard, applying a debuff) grants a
/// temporary tempo boost. It decays over time and is spent in full the next
/// time the combatant acts — a burst reward for aggression, not a permanent buff.
/// </summary>
public static class TempoMomentumCalibration
{
    /// <summary>Cap on accumulated momentum (500 = up to +50% tempo).</summary>
    public const int MaxPerMille = 500;

    public const int CriticalHitGainPerMille = 80;
    public const int GuardBreakGainPerMille = 60;
    public const int DebuffAppliedGainPerMille = 40;

    /// <summary>Momentum decays by 1 per-mille point every this many elapsed ticks.</summary>
    public const int DecayTicksPerPoint = 10;
}
