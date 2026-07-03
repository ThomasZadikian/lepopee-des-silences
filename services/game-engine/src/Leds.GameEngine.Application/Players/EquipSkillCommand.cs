using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed record EquipSkillCommand(Guid PlayerId, Guid CharacterId, string SkillKey) : IRequest<PlayerProfileView>;
