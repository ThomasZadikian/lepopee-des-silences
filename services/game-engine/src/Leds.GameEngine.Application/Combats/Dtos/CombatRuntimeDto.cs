using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatRuntimeDto(
    CombatId Id,
    CombatStatus Status,
    int TurnNumber,
    CombatantId? ActiveCombatantId,
    IReadOnlyCollection<CombatantRuntimeDto> Allies,
    IReadOnlyCollection<CombatantRuntimeDto> Enemies)
{
    public static CombatRuntimeDto FromDomain(Combat combat)
    {
        return new CombatRuntimeDto(
            Id: combat.Id,
            Status: combat.Status,
            TurnNumber: combat.TurnNumber,
            ActiveCombatantId: combat.ActiveCombatantId,
            Allies: combat.Allies
                .Select(CombatantRuntimeDto.FromDomain)
                .ToArray(),
            Enemies: combat.Enemies
                .Select(CombatantRuntimeDto.FromDomain)
                .ToArray());
    }
}
