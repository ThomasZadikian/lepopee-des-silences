using MediatR;

namespace Leds.GameEngine.Application.Combats.SubmitCombatAction;

public sealed record SubmitCombatActionCommand(
    Guid RunId,
    Guid CombatId,
    Guid ActorId,
    Guid TargetId,
    string ActionType)
    : IRequest<SubmitCombatActionResponse>;