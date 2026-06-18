using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.DevTools;

public sealed record DevToolsRunDebugResult(string Message, RunDto Run);

public sealed record DevToolsCombatDebugResult(string Message, CombatRuntimeDto Combat);
