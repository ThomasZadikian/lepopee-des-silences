using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Pure domain service that scales enemy stats by a difficulty multiplier.
/// Deterministic, testable, no external dependencies.
/// </summary>
public sealed class EnemyStatScaler
{
    private const double MinMultiplier = 0.5;
    private const double MaxMultiplier = 3.0;

    public ScaledEnemyStats Scale(int baseVitality, int basePower, double difficultyMultiplier)
    {
        var clampedMultiplier = Math.Clamp(difficultyMultiplier, MinMultiplier, MaxMultiplier);

        var scaledVitality = Math.Max(1, (int)Math.Ceiling(baseVitality * clampedMultiplier));
        var scaledPower = Math.Max(1, (int)Math.Ceiling(basePower * clampedMultiplier));

        return new ScaledEnemyStats(scaledVitality, scaledPower, clampedMultiplier);
    }
}

public sealed record ScaledEnemyStats(int Vitality, int Power, double AppliedMultiplier);
