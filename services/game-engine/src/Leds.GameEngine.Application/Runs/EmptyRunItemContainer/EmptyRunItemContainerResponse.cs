using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.EmptyRunItemContainer;

public sealed record EmptyRunItemContainerResponse(
    Guid RunId,
    Guid ContainerItemId,
    IReadOnlyCollection<RunItemDto> Inventory);
