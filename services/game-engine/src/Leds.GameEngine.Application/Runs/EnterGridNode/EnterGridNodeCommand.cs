using MediatR;

namespace Leds.GameEngine.Application.Runs.EnterGridNode;

public sealed record EnterGridNodeCommand(Guid RunId, Guid NodeId)
    : IRequest<EnterGridNodeResponse>;
