using MediatR;

namespace Leds.GameEngine.Application.Runs.ExitMidRoom;

public sealed record ExitMidRoomCommand(Guid RunId) : IRequest<ExitMidRoomResponse>;
