using MediatR;

namespace Leds.GameEngine.Application.Runs.EmptyRunItemContainer;

public sealed record EmptyRunItemContainerCommand(Guid RunId, Guid ContainerItemId)
    : IRequest<EmptyRunItemContainerResponse>;
