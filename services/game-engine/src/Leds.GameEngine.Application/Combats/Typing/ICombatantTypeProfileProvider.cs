using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Typing;

/// <summary>
/// Resolves the emotional type identity of combatants and the type a given skill
/// deals. Implementations are the integration seam for type/weakness data:
/// today a built-in default table, tomorrow catalog/seed-backed.
/// </summary>
public interface ICombatantTypeProfileProvider
{
    /// <summary>The defender-side profile (attack type + weaknesses) of a combatant.</summary>
    CombatantTypeProfile Resolve(Combatant combatant);

    /// <summary>
    /// The emotional type an attack deals: a per-skill override if the skill
    /// declares one, otherwise the attacker's profile attack type.
    /// </summary>
    EmotionalType ResolveAttackType(Combatant attacker, CombatantSkill skill);
}