using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.PalaceLaws.Ports;

public interface IPalaceIndicatorRepository
{
    Task AddAsync(PalaceIndicator indicator, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PalaceIndicator>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
}
