using MediatR;

namespace Leds.GameEngine.Application.Runs.InteractWithRoomNpc;

public sealed record InteractWithRoomNpcCommand(Guid RunId, Guid RoomNpcId)
    : IRequest<InteractWithRoomNpcResponse>;
