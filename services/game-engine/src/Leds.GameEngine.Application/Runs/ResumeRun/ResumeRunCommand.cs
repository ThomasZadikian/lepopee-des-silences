using MediatR;

namespace Leds.GameEngine.Application.Runs.ResumeRun;

public sealed record ResumeRunCommand(Guid RunId) : IRequest<ResumeRunResponse>;