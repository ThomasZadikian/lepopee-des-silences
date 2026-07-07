using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.PourRunItemLiquid;

public sealed record PourRunItemLiquidResponse(
    Guid RunId,
    Guid ContainerItemId,
    string ContainedLiquidDefinitionKey,
    bool LiquidItemDepleted,
    IReadOnlyCollection<RunItemDto> Inventory);
