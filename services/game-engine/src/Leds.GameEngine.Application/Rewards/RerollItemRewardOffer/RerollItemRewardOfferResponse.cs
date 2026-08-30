using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Rewards.RerollItemRewardOffer;

public sealed record RerollItemRewardOfferResponse(
    RunDto Run,
    RewardOfferDto RewardOffer);
