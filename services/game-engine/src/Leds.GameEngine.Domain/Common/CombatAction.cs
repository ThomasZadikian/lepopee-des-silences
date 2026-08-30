using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed record CombatAction(
    CombatantId ActorId,
    CombatantId TargetId,
    CombatActionType ActionType)
{
    public static CombatAction BasicAttack(
        CombatantId actorId,
        CombatantId targetId)
    {
        if (actorId.Value == Guid.Empty)
        {
            throw new DomainException("Combat action actor id is required.");
        }

        if (targetId.Value == Guid.Empty)
        {
            throw new DomainException("Combat action target id is required.");
        }

        if (actorId == targetId)
        {
            throw new DomainException("A combatant cannot target itself.");
        }

        return new CombatAction(
            actorId,
            targetId,
            CombatActionType.BasicAttack);
    }
}