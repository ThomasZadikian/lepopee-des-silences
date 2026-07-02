using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record SpendStatPointCommand(Guid PlayerId, Guid CharacterId, string Stat) : IRequest<PlayerProfileView>;
