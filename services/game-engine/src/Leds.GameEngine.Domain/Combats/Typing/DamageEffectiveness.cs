namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// How effective an incoming attack type is against a defender's affinities.
/// </summary>
public enum DamageEffectiveness
{
    Neutral = 0,
    Weak = 1,
    Resistant = 2,
    Immune = 3
}

/// <summary>
/// The single source of truth for the weakness/resistance multipliers.
/// Faiblesse ×1.5 / Résistance ×0.75 / Immunité ×0 / Neutre ×1.0.
/// </summary>
public static class DamageEffectivenessMultipliers
{
    public const double Neutral = 1.0;
    public const double Weak = 1.5;
    public const double Resistant = 0.75;
    public const double Immune = 0.0;

    public static double For(DamageEffectiveness effectiveness) => effectiveness switch
    {
        DamageEffectiveness.Weak => Weak,
        DamageEffectiveness.Resistant => Resistant,
        DamageEffectiveness.Immune => Immune,
        _ => Neutral
    };
}