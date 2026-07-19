namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Pure domain service that scales enemy stats by a difficulty multiplier.
/// Deterministic, testable, no external dependencies.
/// </summary>
public sealed class EnemyStatScaler
{
    private const double MinMultiplier = 0.5;

    // Risk tier is the sole difficulty axis now (see RiskTier / CombatRiskProfileResolver) —
    // there are only 5 tiers, so this ceiling is a defensive clamp, not an active bound.
    private const double MaxMultiplier = 5.0;

    public ScaledEnemyStats Scale(int baseVitality, int basePower, double difficultyMultiplier)
    {
        var clampedMultiplier = Math.Clamp(difficultyMultiplier, MinMultiplier, MaxMultiplier);

        var scaledVitality = Math.Max(1, (int)Math.Ceiling(baseVitality * clampedMultiplier));
        var scaledPower = Math.Max(1, (int)Math.Ceiling(basePower * clampedMultiplier));

        return new ScaledEnemyStats(scaledVitality, scaledPower, clampedMultiplier);
    }

    /// <summary>
    /// Scales a single authored stat (e.g. Attack, Defense) by the same
    /// difficulty multiplier applied to Vitality/Power, so enemy stats keep
    /// pace with run depth.
    /// </summary>
    public int ScaleValue(int baseValue, double difficultyMultiplier)
    {
        var clampedMultiplier = Math.Clamp(difficultyMultiplier, MinMultiplier, MaxMultiplier);
        return Math.Max(1, (int)Math.Ceiling(baseValue * clampedMultiplier));
    }
}

public sealed record ScaledEnemyStats(int Vitality, int Power, double AppliedMultiplier);
