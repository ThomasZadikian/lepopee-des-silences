using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunReputation;

public sealed record GetRunReputationQuery(Guid RunId)
    : IRequest<GetRunReputationResponse>;
