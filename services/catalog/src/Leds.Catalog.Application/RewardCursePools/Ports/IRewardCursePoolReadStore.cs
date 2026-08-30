using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.Application.RewardCursePools.Ports;

public interface IRewardCursePoolReadStore
{
    Task<IReadOnlyCollection<IRewardCursePool>> ListActiveAsync(
        CancellationToken cancellationToken);
}