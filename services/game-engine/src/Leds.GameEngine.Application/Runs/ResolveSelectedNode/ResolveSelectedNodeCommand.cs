using MediatR;

namespace Leds.GameEngine.Application.Runs.ResolveSelectedNode;

public sealed record ResolveSelectedNodeCommand(Guid RunId)
    : IRequest<ResolveSelectedNodeResponse>;