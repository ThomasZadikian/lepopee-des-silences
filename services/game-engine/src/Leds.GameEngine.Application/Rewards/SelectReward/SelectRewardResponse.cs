using Leds.GameEngine.Application.Rewards.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Rewards.SelectReward;

public sealed record SelectRewardResponse(
    RunDto Run,
    RewardOfferDto RewardOffer);
