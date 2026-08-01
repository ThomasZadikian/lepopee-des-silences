using MediatR;

namespace Leds.GameEngine.Application.Runs.GetUpcomingRooms;

public sealed record GetUpcomingRoomsQuery(Guid RunId)
    : IRequest<GetUpcomingRoomsResponse>;
