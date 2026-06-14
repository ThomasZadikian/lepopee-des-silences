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
    bool IsUsable)
{
    private static readonly HashSet<RunItemEffectType> UsableEffects =
    [
        RunItemEffectType.Heal,
        RunItemEffectType.Guard,
        RunItemEffectType.ManaRestore,
        RunItemEffectType.ChargeRestore,
    ];

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
        IsUsable: item.Type == RunItemType.Consumable
               && item.Quantity > 0
               && UsableEffects.Contains(item.EffectType));
}