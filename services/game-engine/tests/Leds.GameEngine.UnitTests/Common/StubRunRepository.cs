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
