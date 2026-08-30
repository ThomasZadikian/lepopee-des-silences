using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record UnequipSkillCommand(Guid PlayerId, Guid CharacterId, string SkillKey) : IRequest<PlayerProfileView>;
