using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

/// <summary>
/// Decides what an enemy does on its turn: scores every (skill, target) the actor
/// can afford against the current situation and returns the best plan. Deterministic
/// (no <c>new Random()</c>). Returns <c>null</c> when the actor has no usable action.
/// </summary>
public interface IEnemyActionPlanner
{
    EnemyActionPlan? Plan(Combat combat, Combatant actor);
}
