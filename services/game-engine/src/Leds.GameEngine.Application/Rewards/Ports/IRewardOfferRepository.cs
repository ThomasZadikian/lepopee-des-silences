using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Application.Rewards.Ports;

public interface IRewardOfferRepository
{
    Task AddAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default);

    Task<RewardOffer?> GetByIdAsync(RewardOfferId rewardOfferId, CancellationToken cancellationToken = default);

    Task UpdateAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default);
}