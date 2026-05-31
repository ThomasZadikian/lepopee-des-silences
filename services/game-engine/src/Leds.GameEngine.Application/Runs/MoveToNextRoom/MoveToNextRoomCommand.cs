using MediatR;

namespace Leds.GameEngine.Application.Runs.MoveToNextRoom;

public sealed record MoveToNextRoomCommand(Guid RunId)
    : IRequest<MoveToNextRoomResponse>;