using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record EquipItemCommand(Guid PlayerId, Guid CharacterId, string ItemKey) : IRequest<PlayerProfileView>;
