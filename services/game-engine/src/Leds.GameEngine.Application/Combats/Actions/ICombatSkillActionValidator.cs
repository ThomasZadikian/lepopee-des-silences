using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Actions;

public interface ICombatSkillActionValidator
{
    CombatSkillActionValidationResult Validate(
        Combat combat,
        Guid actorId,
        string skillKey,
        IReadOnlyCollection<Guid> targetIds);
}
