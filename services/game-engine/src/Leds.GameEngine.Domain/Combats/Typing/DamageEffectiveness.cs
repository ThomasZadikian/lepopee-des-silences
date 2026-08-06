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
