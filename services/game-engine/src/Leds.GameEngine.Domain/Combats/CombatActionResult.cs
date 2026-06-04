namespace Leds.GameEngine.Domain.Combats;

public sealed record CombatActionResult(
    CombatId CombatId,
    CombatantId ActorId,
    CombatantId TargetId,
    CombatActionType ActionType,
    int Damage,
    int TargetRemainingHealth,
    bool TargetDefeated,
    CombatState CombatState,
    CombatantSide? WinningSide,
    CombatantId? NextActorId,
    int Round);