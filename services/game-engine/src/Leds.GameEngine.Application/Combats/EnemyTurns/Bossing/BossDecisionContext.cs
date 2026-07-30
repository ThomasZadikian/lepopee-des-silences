using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;

/// <summary>
/// The state handed to an <see cref="IBossBehavior"/> when it is asked to decide
/// the boss's action. Exposes the live <see cref="Combat"/> and the boss
/// <see cref="Combatant"/> so behaviors can branch on HP thresholds, turn number,
/// living allies/enemies, guard, charge, etc. to drive phases and conditional
/// patterns.
/// </summary>
/// <param name="Combat">The active combat aggregate (read-only intent — behaviors must not mutate it).</param>
/// <param name="Boss">The boss combatant whose turn is being resolved.</param>
public sealed record BossDecisionContext(ICombatContext Combat, Combatant Boss);
