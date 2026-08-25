using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunRepository
{
    Task AddAsync(Run run, CancellationToken cancellationToken);

    Task<Run?> GetByIdAsync(RunId runId, CancellationToken cancellationToken);

    Task<bool> HasActiveOrSuspendedAsync(Guid playerId, CancellationToken cancellationToken);

    Task UpdateAsync(Run run, CancellationToken cancellationToken);

}
