using Leds.GameEngine.Application.Combats.Dtos;
using MediatR;

namespace Leds.GameEngine.Application.Runs.TacticalCombat;

public sealed record GetCurrentTacticalCombatQuery(Guid RunId)
    : IRequest<TacticalCombatRuntimeDto>;
