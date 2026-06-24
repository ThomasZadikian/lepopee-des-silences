namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Pure, deterministic ATB rule math: charge reward, post-action recovery,
/// interruption push and opening gauge. No state, no RNG.
/// </summary>
public static class AtbActionMath
{
    /// <summary>
    /// Incremental charge damage multiplier from a (possibly overflowed) gauge,
    /// in [1.0, <see cref="AtbCalibration.ChargeMaxMultiplier"/>]. Three light tiers,
    /// capped at ×1.5.
    /// </summary>
    public static double ChargeDamageMultiplier(int gauge)
    {
        var overflow = Math.Clamp(gauge - AtbConstants.ReadyThreshold, 0, AtbConstants.MaxChargeOverflow);
        if (overflow <= 0)
        {
            return 1.0;
        }

        var ratio = (double)overflow / AtbConstants.MaxChargeOverflow;
        if (ratio < 0.34)
        {
            return 1.15;
        }

        if (ratio < 0.67)
        {
            return 1.30;
        }

        return AtbCalibration.ChargeMaxMultiplier;
    }

    /// <summary>
    /// Post-action recovery in ticks: heavier skills (higher base power) cost more,
    /// a higher Recovery stat mitigates it. Always ≥ 0.
    /// </summary>
    public static int RecoveryTicks(int basePower, int recoveryStat)
    {
        if (basePower <= 0)
        {
            return 0;
        }

        var mitigated = Math.Max(1, recoveryStat);
        return basePower * AtbCalibration.BaseRecoveryPerPower * AtbCalibration.RecoveryStatReference / mitigated;
    }

    /// <summary>
    /// Resulting target gauge after a stagger / interruption. A target that was
    /// charging (gauge ≥ threshold) suffers a larger push and loses its charge.
    /// </summary>
    public static int Interrupt(int targetGauge)
    {
        var push = targetGauge >= AtbConstants.ReadyThreshold
            ? AtbCalibration.StaggerPushVsCharging
            : AtbCalibration.StaggerPushBase;

        return Math.Max(0, targetGauge - push);
    }

    /// <summary>
    /// Opening gauge from Initiative plus a Markov bias, clamped below the threshold
    /// so no one opens already-ready.
    /// </summary>
    public static int InitialGauge(int initiative, int markovOpeningBias)
    {
        var raw = initiative * AtbConstants.InitiativeScale + markovOpeningBias;
        return Math.Clamp(raw, 0, AtbConstants.ReadyThreshold - 1);
    }
}