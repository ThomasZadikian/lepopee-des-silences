using MediatR;

namespace Leds.Player.Application.Players.AwardStatPoint;

public sealed record AwardStatPointCommand(Guid PlayerId, int Amount = 1) : IRequest<PlayerProfileDto>;
