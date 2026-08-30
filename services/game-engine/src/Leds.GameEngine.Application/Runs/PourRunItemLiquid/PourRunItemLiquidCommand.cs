using MediatR;

namespace Leds.GameEngine.Application.Runs.PourRunItemLiquid;

public sealed record PourRunItemLiquidCommand(Guid RunId, Guid ContainerItemId, Guid LiquidItemId)
    : IRequest<PourRunItemLiquidResponse>;
