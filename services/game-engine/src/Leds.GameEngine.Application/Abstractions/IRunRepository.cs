using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Abstractions;

public interface IRunRepository
{
    Task AddAsync(Run run, CancellationToken cancellationToken);
}