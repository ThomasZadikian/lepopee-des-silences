using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Targeting;

public interface ICombatTargetingRuleValidator
{
    CombatTargetingValidationResult Validate(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Guid> targetIds);
}
