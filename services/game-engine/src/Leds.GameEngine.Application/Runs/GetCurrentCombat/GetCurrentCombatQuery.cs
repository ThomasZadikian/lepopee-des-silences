using Leds.GameEngine.Application.Combats.Dtos;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetCurrentCombat;

public sealed record GetCurrentCombatQuery(Guid RunId) : IRequest<CombatRuntimeDto>;
