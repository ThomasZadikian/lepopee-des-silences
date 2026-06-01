using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed record ResolveCurrentEventResponse(
    RunDto Run,
    ResolvedNodeEventOutcomeDto Outcome);