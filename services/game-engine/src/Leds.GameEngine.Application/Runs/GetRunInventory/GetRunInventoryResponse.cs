using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.GetRunInventory;

public sealed record GetRunInventoryResponse(
    Guid RunId,
    IReadOnlyCollection<RunItemDto> Items);

public sealed record RunItemDto(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    string Description,
    string Type,
    string Rarity,
    int Quantity,
    string EffectType,
    int EffectAmount,
    bool IsUsable,
    bool IsBattleItem,
    bool IsContainer = false,
    int? ContainerCapacity = null,
    bool IsLiquid = false,
    string? ContainedLiquidDefinitionKey = null)
{
    public static RunItemDto FromDomain(RunItem item) => new(
        item.Id.Value,
        item.DefinitionKey,
        item.DisplayName,
        item.Description,
        item.Type.ToString(),
        item.Rarity.ToString(),
        item.Quantity,
        item.EffectType.ToString(),
        item.EffectAmount,
        item.IsUsable,
        item.IsBattleItem,
        item.IsContainer,
        item.ContainerCapacity,
        item.IsLiquid,
        item.ContainedLiquidDefinitionKey);
}