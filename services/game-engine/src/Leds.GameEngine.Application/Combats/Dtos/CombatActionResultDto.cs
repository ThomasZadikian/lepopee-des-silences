using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatActionResultDto(
    Guid CombatId,
    Guid ActorId,
    Guid TargetId,
    string ActionType,
    int Damage,
    int TargetRemainingHealth,
    bool TargetDefeated,
    string CombatState,
    string? WinningSide,
    Guid? NextActorId,
    int Round)
{
    public static CombatActionResultDto FromDomain(CombatActionResult result)
    {
        return new CombatActionResultDto(
            result.CombatId.Value,
            result.ActorId.Value,
            result.TargetId.Value,
            result.ActionType.ToString(),
            result.Damage,
            result.TargetRemainingHealth,
            result.TargetDefeated,
            result.CombatState.ToString(),
            result.WinningSide?.ToString(),
            result.NextActorId?.Value,
            result.Round);
    }
}