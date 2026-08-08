using MediatR;

namespace Leds.GameEngine.Application.Runs.ConfirmRoomExit;

public sealed record ConfirmRoomExitCommand(Guid RunId, Guid NodeId)
    : IRequest<ConfirmRoomExitResponse>;
