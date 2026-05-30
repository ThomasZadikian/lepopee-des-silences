using MediatR;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed record ResolveCurrentEventCommand(Guid RunId)
    : IRequest<ResolveCurrentEventResponse>;