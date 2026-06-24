namespace Leds.GameEngine.Domain.Combats.Atb;

/// <summary>
/// Single tuning surface for the ATB rules (charge reward, action recovery,
/// interruption pushes). Gauge units are relative to
/// <see cref="AtbConstants.ReadyThreshold"/> (10000).
/// </summary>
public static class AtbCalibration
{
    /// <summary>Hard cap on the charge/overflow damage multiplier.</summary>
    public const double ChargeMaxMultiplier = 1.5;

    /// <summary>Recovery ticks per point of skill power, before stat mitigation.</summary>
    public const int BaseRecoveryPerPower = 4;

    /// <summary>Recovery stat at which a combatant pays the base recovery (higher reduces it).</summary>
    public const int RecoveryStatReference = 5;

    /// <summary>Gauge pushed back by a tagged stagger skill on a non-charging target.</summary>
    public const int StaggerPushBase = 3_000;

    /// <summary>Larger push when the target was charging (cancels its charge — anti-charge).</summary>
    public const int StaggerPushVsCharging = 6_000;
}