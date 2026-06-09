using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Effects;

public interface ICombatSkillEffectResolver
{
    CombatSkillEffectResolution Resolve(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets);
}
