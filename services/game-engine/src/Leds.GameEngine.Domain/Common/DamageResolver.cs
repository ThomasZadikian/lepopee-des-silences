using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class DamageResolver
{
    public DamageResult ResolveBasicAttack(
        CombatantSnapshot attacker,
        CombatantSnapshot defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        if (attacker.IsDefeated)
        {
            throw new DomainException("Defeated combatant cannot attack.");
        }

        if (defender.IsDefeated)
        {
            throw new DomainException("Defeated combatant cannot be targeted.");
        }

        var rawPower = attacker.Attack;
        var mitigatedByDefense = defender.Defense;
        var damage = Math.Max(1, rawPower - mitigatedByDefense);

        return new DamageResult(
            attacker.Id,
            defender.Id,
            rawPower,
            mitigatedByDefense,
            damage);
    }
}