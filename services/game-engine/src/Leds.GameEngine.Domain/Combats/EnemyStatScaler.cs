namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Pure domain service that scales enemy stats by a difficulty multiplier.
/// Deterministic, testable, no external dependencies.
/// </summary>
public sealed class EnemyStatScaler
{
    private const double MinMultiplier = 0.5;

    // Endless run: no hard upper cap on scaling (difficulty grows with run depth).
    // A high ceiling only guards against pathological values; it does not bound normal play.
    private const double MaxMultiplier = 1000.0;

    // Endless difficulty ramp: room 1 = base difficulty, +DepthDifficultyStep per room.
    // BALANCE KNOB: tune this to set how fast the run gets harder with depth.
    public const double DepthDifficultyStep = 0.5;

    /// <summary>
    /// Difficulty multiplier derived from the run depth (room number).
    /// Room 1 -> 1.0, then +DepthDifficultyStep per room, unbounded.
    /// </summary>
    public static double DepthMultiplier(int roomDepth)
    {
        var effectiveDepth = Math.Max(0, roomDepth - 1);
        return 1.0 + (effectiveDepth * DepthDifficultyStep);
    }

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
