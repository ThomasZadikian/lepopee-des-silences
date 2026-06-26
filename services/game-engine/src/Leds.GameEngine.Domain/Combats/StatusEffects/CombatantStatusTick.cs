namespace Leds.GameEngine.Domain.Combats.StatusEffects;

/// <summary>A status effect tick on a specific combatant — surfaced for combat logs.</summary>
public sealed record CombatantStatusTick(
    System.Guid CombatantId,
    string CombatantName,
    StatusTickEvent Event);
