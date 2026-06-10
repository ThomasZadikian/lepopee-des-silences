using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatRuntimeDto(
    Guid Id,
    string Status,
    int TurnNumber,
    Guid? ActiveCombatantId,
    IReadOnlyCollection<CombatantRuntimeDto> Allies,
    IReadOnlyCollection<CombatantRuntimeDto> Enemies)
{
    public static CombatRuntimeDto FromDomain(Combat combat)
    {
        return new CombatRuntimeDto(
            Id: combat.Id.Value,
            Status: combat.Status.ToString(),
            TurnNumber: combat.TurnNumber,
            ActiveCombatantId: combat.ActiveCombatantId?.Value,
            Allies: combat.Allies
                .Select(CombatantRuntimeDto.FromDomain)
                .ToArray(),
            Enemies: combat.Enemies
                .Select(CombatantRuntimeDto.FromDomain)
                .ToArray());
    }
}
