using MediatR;

namespace Leds.Player.Application.Players.EquipItem;

public sealed record EquipItemCommand(Guid PlayerId, Guid CharacterId, string ItemKey) : IRequest<PlayerProfileDto>;
