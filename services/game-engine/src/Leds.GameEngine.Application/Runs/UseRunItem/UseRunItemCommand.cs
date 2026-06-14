using MediatR;

namespace Leds.GameEngine.Application.Runs.UseRunItem;

public sealed record UseRunItemCommand(Guid RunId, Guid ItemId)
    : IRequest<UseRunItemResponse>;