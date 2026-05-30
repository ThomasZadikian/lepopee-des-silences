using MediatR;

namespace Leds.GameEngine.Application.Runs.ChooseNode;

public sealed record ChooseNodeCommand(
    Guid RunId,
    Guid NodeId) : IRequest<ChooseNodeResponse>;