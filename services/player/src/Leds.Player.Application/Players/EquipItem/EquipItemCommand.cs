using MediatR;

using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Players.EquipItem;

public sealed record EquipItemCommand(
    Guid PlayerId,
    Guid CharacterId,
    string ItemKey,
    EquipmentSlotKind Slot = EquipmentSlotKind.Relic)
    : IRequest<PlayerProfileDto>;
