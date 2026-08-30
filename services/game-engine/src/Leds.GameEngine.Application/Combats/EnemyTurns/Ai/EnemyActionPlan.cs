using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

/// <summary>
/// A concrete action an enemy intends to take this turn: a skill, its target(s),
/// and the utility score that won the evaluation. Produced by
/// <see cref="IEnemyActionPlanner"/> and consumed by the enemy turn resolver.
/// </summary>
public sealed record EnemyActionPlan(
    CombatantSkill Skill,
    IReadOnlyCollection<Guid> TargetIds,
    double Score);
