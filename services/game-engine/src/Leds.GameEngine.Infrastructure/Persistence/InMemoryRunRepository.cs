using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Runs;

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
}