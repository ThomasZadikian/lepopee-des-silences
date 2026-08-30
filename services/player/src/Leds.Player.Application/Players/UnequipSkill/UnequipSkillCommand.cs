using MediatR;

namespace Leds.Player.Application.Players.UnequipSkill;

public sealed record UnequipSkillCommand(Guid PlayerId, Guid CharacterId, string SkillKey) : IRequest<PlayerProfileDto>;
