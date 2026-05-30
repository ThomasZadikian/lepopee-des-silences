using MediatR;

namespace Leds.GameEngine.Application.Runs.ProgressRun;

public sealed record ProgressRunCommand(Guid RunId)
    : IRequest<ProgressRunResponse>;