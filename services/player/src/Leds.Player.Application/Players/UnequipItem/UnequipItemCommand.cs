using MediatR;

namespace Leds.Player.Application.Players.UnequipItem;

public sealed record UnequipItemCommand(Guid PlayerId, Guid CharacterId, string ItemKey) : IRequest<PlayerProfileDto>;
