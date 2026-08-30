using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveParty;

public sealed record MovePartyCommand(Guid RunId, int TargetX, int TargetY)
    : IRequest<MovePartyResponse>;
