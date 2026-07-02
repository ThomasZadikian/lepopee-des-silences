using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record GetPlayerProfileQuery(Guid PlayerId) : IRequest<PlayerProfileView>;
