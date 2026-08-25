using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common;

public sealed class StubRunRepository : IRunRepository
{
    private readonly Dictionary<Guid, Run> _runs = new();

    public Task AddAsync(Run run, CancellationToken cancellationToken)
    {
        _runs[run.Id.Value] = run;
        return Task.CompletedTask;
    }

    public Task<Run?> GetByIdAsync(RunId runId, CancellationToken cancellationToken)
    {
        _runs.TryGetValue(runId.Value, out var run);
        return Task.FromResult(run);
    }

    public Task<bool> HasActiveOrSuspendedAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var exists = _runs.Values.Any(run =>
            run.PlayerId == playerId &&
            run.Status is RunStatus.Active or RunStatus.Suspended);
        return Task.FromResult(exists);
    }

    public Task UpdateAsync(Run run, CancellationToken cancellationToken)
    {
        _runs[run.Id.Value] = run;
        return Task.CompletedTask;
    }

    public Task UpdateActiveCombatStateAsync(Run run, CancellationToken cancellationToken)
    {
        _runs[run.Id.Value] = run;
        return Task.CompletedTask;
    }
}
