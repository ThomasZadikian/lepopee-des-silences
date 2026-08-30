using MediatR;

namespace Leds.Player.Application.Players.EquipSkill;

public sealed record EquipSkillCommand(Guid PlayerId, Guid CharacterId, string SkillKey) : IRequest<PlayerProfileDto>;
