using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats;

public static class CombatItemHelper
{
    /// <summary>
    /// Projette les objets de run utilisables en combat vers leurs DTOs.
    /// Appelé par tout handler qui retourne un <see cref="CombatRuntimeDto"/>.
    /// </summary>
    public static IReadOnlyCollection<CombatUsableItemDto> GetUsableBattleItems(Run run)
    {
        return run.RunItems
            .Where(i => i.IsUsable && i.IsBattleItem)
            .Select(i => new CombatUsableItemDto(
                i.Id.Value,
                i.DefinitionKey,
                i.DisplayName,
                i.EffectType.ToString(),
                i.EffectAmount,
                i.Quantity,
                i.BattleTargetingType))
            .ToArray();
    }
}