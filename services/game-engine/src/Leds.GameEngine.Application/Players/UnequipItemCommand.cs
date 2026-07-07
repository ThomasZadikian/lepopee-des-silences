using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record UnequipItemCommand(Guid PlayerId, Guid CharacterId, string ItemKey) : IRequest<PlayerProfileView>;
