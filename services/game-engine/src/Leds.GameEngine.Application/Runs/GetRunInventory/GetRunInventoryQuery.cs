using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunInventory;

public sealed record GetRunInventoryQuery(Guid RunId)
    : IRequest<GetRunInventoryResponse>;
