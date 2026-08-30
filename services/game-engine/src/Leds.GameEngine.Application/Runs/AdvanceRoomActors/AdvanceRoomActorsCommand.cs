using Leds.GameEngine.Domain.Rooms;
using MediatR;

namespace Leds.GameEngine.Application.Runs.AdvanceRoomActors;

public sealed record AdvanceRoomActorsCommand(Guid RunId, ActorAdvanceMode Mode)
    : IRequest<AdvanceRoomActorsResponse>;
