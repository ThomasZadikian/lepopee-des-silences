using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Runs;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Persistence;

public sealed class InMemoryRunRepository : IRunRepository
{
    private readonly Dictionary<RunId, Run> _runs = [];

    public Task AddAsync(Run run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        _runs.Add(run.Id, run);

        return Task.CompletedTask;
    }
    public Task<Run?> GetByIdAsync(RunId runId, CancellationToken cancellationToken)
    {
        _runs.TryGetValue(runId, out var run);

        return Task.FromResult(run);
    }
}