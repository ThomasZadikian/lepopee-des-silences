using MediatR;

namespace Leds.GameEngine.Application.Runs.SaveAndExitRun;

public sealed record SaveAndExitRunCommand(Guid RunId) : IRequest<SaveAndExitRunResponse>;