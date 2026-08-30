using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Effects;

public interface ICombatSkillEffectResolver
{
    CombatSkillEffectResolution Resolve(
        ICombatContext combat,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets);
}