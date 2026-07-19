using MediatR;

namespace Leds.GameEngine.Application.Runs.ChallengeBossRemotely;

public sealed record ChallengeBossRemotelyCommand(Guid RunId)
    : IRequest<ChallengeBossRemotelyResponse>;
