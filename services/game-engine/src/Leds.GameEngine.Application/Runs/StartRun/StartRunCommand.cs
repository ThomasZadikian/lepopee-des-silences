using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed record StartRunCommand(Guid PlayerId) : IRequest<StartRunResponse>;