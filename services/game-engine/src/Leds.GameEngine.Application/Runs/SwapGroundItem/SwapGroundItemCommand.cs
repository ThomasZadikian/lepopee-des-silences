using MediatR;

namespace Leds.GameEngine.Application.Runs.SwapGroundItem;

public sealed record SwapGroundItemCommand(
    Guid RunId,
    Guid GroundItemId,
    Guid HeldItemId) : IRequest<SwapGroundItemResponse>;
