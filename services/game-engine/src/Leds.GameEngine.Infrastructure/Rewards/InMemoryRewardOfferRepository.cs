using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.Infrastructure.Rewards;

public sealed class InMemoryRewardOfferRepository : IRewardOfferRepository
{
    private readonly Dictionary<RewardOfferId, RewardOffer> _offers = [];

    public Task AddAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rewardOffer);

        _offers.Add(rewardOffer.Id, rewardOffer);

        return Task.CompletedTask;
    }

    public Task<RewardOffer?> GetByIdAsync(RewardOfferId rewardOfferId, CancellationToken cancellationToken = default)
    {
        _offers.TryGetValue(rewardOfferId, out var offer);

        return Task.FromResult(offer);
    }

    public Task UpdateAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rewardOffer);

        _offers[rewardOffer.Id] = rewardOffer;

        return Task.CompletedTask;
    }
}
