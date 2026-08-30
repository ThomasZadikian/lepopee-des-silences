using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Rewards.Ports;

public interface IRewardOfferRepository
{
    Task AddAsync(RunId runId, RewardOffer rewardOffer, CancellationToken cancellationToken = default);

    Task<RewardOffer?> GetByIdAsync(RewardOfferId rewardOfferId, CancellationToken cancellationToken = default);

    Task UpdateAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default);
}
