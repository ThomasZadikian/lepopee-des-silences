using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.SpendStatPoint;

public sealed record SpendStatPointCommand(Guid PlayerId, Guid CharacterId, PlayerStatKind Stat) : IRequest<PlayerProfileDto>;
