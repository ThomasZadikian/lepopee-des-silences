using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.PalaceLaws.Ports;

public interface IAdaptiveInfluenceRepository
{
    Task AddAsync(AdaptiveInfluence influence, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdaptiveInfluence>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
}

public interface IPalaceIndicatorRepository
{
    Task AddAsync(PalaceIndicator indicator, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PalaceIndicator>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
}
