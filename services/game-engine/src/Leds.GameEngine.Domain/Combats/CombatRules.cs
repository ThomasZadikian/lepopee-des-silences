namespace Leds.GameEngine.Domain.Combats;

/// <summary>Canonical constants shared by tactical combat resolution.</summary>
public static class CombatRules
{
    public const int HitCounterTrigger = 13;
    public const int LowHpDamageAmplificationThresholdPercent = 25;
    public const int LowHpDamageAmplificationBonusPercent = 15;
    public const int DuelSingleTargetBonusPercent = 20;
    public const int DuelAreaOfEffectPenaltyPercent = 20;
}
