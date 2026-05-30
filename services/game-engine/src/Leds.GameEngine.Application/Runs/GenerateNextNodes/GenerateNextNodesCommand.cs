using MediatR;

namespace Leds.GameEngine.Application.Runs.GenerateNextNodes;

public sealed record GenerateNextNodesCommand(Guid RunId)
    : IRequest<GenerateNextNodesResponse>;