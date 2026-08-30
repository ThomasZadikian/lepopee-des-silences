namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Single tuning surface for critical hits. Critical hits are driven by the
/// attacker's Focus stat and are fully deterministic (no RNG) — see
/// <see cref="DeterministicCombatRoll"/>.
/// </summary>
public static class CriticalHitCalibration
{
    /// <summary>Critical chance gained per point of Focus (0.01 = 1% per Focus).</summary>
    public const double CritChancePerFocus = 0.01;

    /// <summary>Hard cap on critical chance regardless of Focus.</summary>
    public const double MaxCritChance = 0.50;

    /// <summary>Damage multiplier applied on a critical hit.</summary>
    public const double CritMultiplier = 1.5;

    /// <summary>Canonical critical multiplier for tactical combat.</summary>
    public const double TacticalCritMultiplier = 1.6;

    /// <summary>
    /// Maps an attacker's Focus to a critical chance in [0, <see cref="MaxCritChance"/>].
    /// </summary>
    public static double CritChanceFromFocus(int focus)
    {
        if (focus <= 0)
        {
            return 0.0;
        }

        var chance = focus * CritChancePerFocus;
        return chance > MaxCritChance ? MaxCritChance : chance;
    }
}
