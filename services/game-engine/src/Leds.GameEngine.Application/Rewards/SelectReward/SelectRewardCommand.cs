using MediatR;

namespace Leds.GameEngine.Application.Rewards.SelectReward;

public sealed record SelectRewardCommand(
    Guid RunId,
    Guid ChoiceId) : IRequest<SelectRewardResponse>;
