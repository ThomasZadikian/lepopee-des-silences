namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>A durable status effect on a combatant, surfaced for the combat UI.</summary>
public sealed record CombatantStatusEffectDto(
    string Key,
    string DisplayName,
    string Kind,   // DamageOverTime | HealOverTime | StatModifier | Stun | Silence | AtbLock
    string Stat,   // AttackPower | Defense | Speed | Focus | None
    int Magnitude,
    int Stacks);
