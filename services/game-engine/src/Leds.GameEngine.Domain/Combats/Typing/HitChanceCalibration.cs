namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Single tuning surface for the hit/miss roll on damaging skills. Deterministic,
/// like <see cref="CriticalHitCalibration"/> — see <see cref="DeterministicCombatRoll"/>.
/// </summary>
public static class HitChanceCalibration
{
    /// <summary>Base chance to hit, before any equipment/effect bonus.</summary>
    public const double BaseHitChance = 0.90;

    /// <summary>
    /// Maps an attacker's equipment-driven hit chance bonus (percentage points,
    /// e.g. 10 for Lunettes d'érudit) onto the final hit chance, capped at 100%.
    /// </summary>
    public static double HitChanceFromBonus(int bonusPercent)
    {
        var chance = BaseHitChance + bonusPercent / 100.0;
        return chance > 1.0 ? 1.0 : chance;
    }
}
