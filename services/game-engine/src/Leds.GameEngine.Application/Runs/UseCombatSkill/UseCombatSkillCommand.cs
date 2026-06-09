using Leds.GameEngine.Application.Combats.Actions;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCombatSkill;

public sealed record UseCombatSkillCommand(
    Guid RunId,
    Guid CombatId,
    Guid ActorId,
    string SkillKey,
    IReadOnlyCollection<Guid> TargetIds)
    : IRequest<CombatSkillActionResult>;
