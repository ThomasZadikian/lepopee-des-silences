using MediatR;

namespace Leds.Player.Application.Players;

public sealed record GetPlayerProfileByIdQuery(Guid PlayerId) : IRequest<PlayerProfileDto?>;
