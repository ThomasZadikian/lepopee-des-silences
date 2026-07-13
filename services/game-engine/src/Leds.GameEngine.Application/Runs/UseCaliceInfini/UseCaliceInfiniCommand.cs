using MediatR;

namespace Leds.GameEngine.Application.Runs.UseCaliceInfini;

public sealed record UseCaliceInfiniCommand(Guid RunId, Guid? TargetCombatantId)
    : IRequest<UseCaliceInfiniResponse>;
