using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatRuntimeDto(
    Guid Id,
    string Status,
    int TurnNumber,
    int CurrentTick,
    Guid? ActiveCombatantId,
    IReadOnlyCollection<CombatantRuntimeDto> Allies,
    IReadOnlyCollection<CombatantRuntimeDto> Enemies,
    IReadOnlyCollection<CombatUsableItemDto> UsableBattleItems)
{
    public static CombatRuntimeDto FromDomain(
        Combat combat,
        IReadOnlyCollection<CombatUsableItemDto>? usableItems = null)
    {
        return new CombatRuntimeDto(
            Id: combat.Id.Value,
            Status: combat.Status.ToString(),
            TurnNumber: combat.TurnNumber,
            CurrentTick: combat.CurrentTick,
            ActiveCombatantId: combat.ActiveCombatantId?.Value,
            Allies: combat.Allies
                .Select(c => CombatantRuntimeDto.FromDomain(c, combat.CurrentTick))
                .ToArray(),
            Enemies: combat.Enemies
                .Select(c => CombatantRuntimeDto.FromDomain(c, combat.CurrentTick))
                .ToArray(),
            UsableBattleItems: usableItems ?? []);
    }
}