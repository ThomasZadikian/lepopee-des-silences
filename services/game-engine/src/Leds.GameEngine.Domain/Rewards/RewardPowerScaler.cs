using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Rewards;

/// <summary>
/// Pure domain service that scales reward amounts by a reward power multiplier.
/// Deterministic, testable, no external dependencies.
/// </summary>
public sealed class RewardPowerScaler
{
    private const double MinMultiplier = 0.5;
    private const double MaxMultiplier = 3.0;

    public int ScaleAmount(int baseAmount, double rewardPowerMultiplier)
    {
        var clampedMultiplier = Math.Clamp(rewardPowerMultiplier, MinMultiplier, MaxMultiplier);
        var scaled = (int)Math.Ceiling(baseAmount * clampedMultiplier);
        return Math.Max(1, scaled);
    }
}
