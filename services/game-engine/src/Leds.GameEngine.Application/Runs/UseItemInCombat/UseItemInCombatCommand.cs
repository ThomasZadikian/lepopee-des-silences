using Leds.GameEngine.Application.Combats.Actions;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseItemInCombat;

public sealed record UseItemInCombatCommand(
    Guid RunId,
    Guid CombatId,
    Guid ItemId,
    IReadOnlyCollection<Guid> TargetIds)
    : IRequest<CombatSkillActionResult>;