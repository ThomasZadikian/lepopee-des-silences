using MediatR;

namespace Leds.Player.Application.Players.UnlockSkill;

public sealed record UnlockSkillCommand(Guid PlayerId, Guid CharacterId, string SkillKey, string? Source) : IRequest<PlayerProfileDto>;
