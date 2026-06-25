using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunRepository
{
    Task AddAsync(Run run, CancellationToken cancellationToken);

    Task<Run?> GetByIdAsync(RunId runId, CancellationToken cancellationToken);

    Task UpdateAsync(Run run, CancellationToken cancellationToken);

    /// <summary>
    /// Hot-path persistence for the real-time ATB clock: writes ONLY the active
    /// combat subtree (combat scalars + combatants + runtime state) in place,
    /// without loading or rewriting the rest of the run aggregate. Use for
    /// mid-combat ticks (combat still active, no run-level change); fall back to
    /// <see cref="UpdateAsync"/> when the combat ends or run state changes.
    /// </summary>
    Task UpdateActiveCombatStateAsync(Run run, CancellationToken cancellationToken);
}