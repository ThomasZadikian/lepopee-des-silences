using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

public sealed record UseTacticalItemCommand(
    Guid RunId,
    Guid ItemId,
    int TargetX,
    int TargetY,
    Guid? TargetCombatantId = null)
    : IRequest<TacticalCombatResponse>;
