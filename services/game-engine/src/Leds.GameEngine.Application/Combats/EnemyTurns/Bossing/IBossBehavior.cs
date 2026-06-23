namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;

/// <summary>
/// A scripted decision policy for a single room boss, identified by the boss
/// combatant's <see cref="Domain.Combats.Combatant.SourceKey"/>.
/// <para>
/// When the active enemy's source key matches <see cref="BossKey"/>, the
/// <see cref="EnemyCombatTurnResolver"/> asks the behavior to decide the action
/// to play. Returning <c>null</c> from <see cref="DecideAction"/> defers that
/// turn back to the generic enemy AI, so a behavior can stay partial (e.g. only
/// override on certain phases) without re-implementing the whole turn.
/// </para>
/// <para>
/// Behaviors must be deterministic for a given <see cref="BossDecisionContext"/>:
/// combat is server-authoritative and replays must be reproducible.
/// </para>
/// </summary>
public interface IBossBehavior
{
    /// <summary>
    /// The boss combatant source key this behavior scripts (e.g.
    /// "boss.memory.archivist"). Matched case-insensitively against
    /// <see cref="Domain.Combats.Combatant.SourceKey"/>.
    /// </summary>
    string BossKey { get; }

    /// <summary>
    /// Decides the action the boss plays on its turn, or returns <c>null</c> to
    /// fall back to the generic enemy turn resolution for this turn.
    /// </summary>
    BossActionDecision? DecideAction(BossDecisionContext context);
}
