using MediatR;

namespace Leds.GameEngine.Application.Runs.RaiseNodeRisk;

public sealed record RaiseNodeRiskCommand(Guid RunId, Guid NodeId)
    : IRequest<RaiseNodeRiskResponse>;
