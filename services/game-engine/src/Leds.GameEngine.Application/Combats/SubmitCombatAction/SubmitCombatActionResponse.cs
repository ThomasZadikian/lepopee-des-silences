using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Combats.SubmitCombatAction;

public sealed record SubmitCombatActionResponse(
    RunDto Run,
    CombatActionResultDto Result);
