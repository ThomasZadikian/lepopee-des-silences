using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Infrastructure.PalaceLaws;

public sealed class InMemoryAdaptiveInfluenceRepository : IAdaptiveInfluenceRepository
{
    private readonly List<AdaptiveInfluence> _influences = [];

    public Task AddAsync(AdaptiveInfluence influence, CancellationToken cancellationToken = default)
    {
        _influences.Add(influence);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AdaptiveInfluence>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<AdaptiveInfluence>>(
            _influences.Where(i => i.RunId == runId).ToList().AsReadOnly());
    }
}

public sealed class InMemoryPalaceIndicatorRepository : IPalaceIndicatorRepository
{
    private readonly List<PalaceIndicator> _indicators = [];

    public Task AddAsync(PalaceIndicator indicator, CancellationToken cancellationToken = default)
    {
        _indicators.Add(indicator);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<PalaceIndicator>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<PalaceIndicator>>(
            _indicators.Where(i => i.RunId == runId).ToList().AsReadOnly());
    }
}
