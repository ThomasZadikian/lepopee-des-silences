using MediatR;

namespace Leds.GameEngine.Application.Runs.GetOpenRunForPlayer;

public sealed record GetOpenRunForPlayerQuery(Guid PlayerId)
    : IRequest<GetOpenRunForPlayerResponse>;
