using MediatR;

namespace Leds.Player.Application.Players.UnlockDifficultyLevel;

public sealed record UnlockDifficultyLevelCommand(Guid PlayerId, int Level)
    : IRequest<PlayerProfileDto>;
