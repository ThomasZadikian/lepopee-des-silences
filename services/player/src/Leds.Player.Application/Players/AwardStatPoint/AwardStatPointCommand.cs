using MediatR;

namespace Leds.Player.Application.Players.AwardStatPoint;

public sealed record AwardStatPointCommand(Guid PlayerId) : IRequest<PlayerProfileDto>;
