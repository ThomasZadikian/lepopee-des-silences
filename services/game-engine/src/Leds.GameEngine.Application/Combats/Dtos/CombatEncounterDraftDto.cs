using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatEncounterDraftDto(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    double DifficultyMultiplier,
    IReadOnlyCollection<CombatEncounterDraftEnemyDto> Enemies,
    IReadOnlyCollection<CombatEncounterDraftAllyDto> Allies)
{
    public static CombatEncounterDraftDto FromDomain(CombatEncounterDraft draft)
    {
        return new CombatEncounterDraftDto(
            RunId: draft.RunId,
            RoomId: draft.RoomId,
            NodeId: draft.NodeId,
            RoomType: draft.RoomType,
            RoomIndex: draft.RoomIndex,
            RiskLevel: draft.RiskLevel,
            EncounterType: draft.EncounterType,
            DifficultyMultiplier: draft.DifficultyMultiplier,
            Enemies: draft.Enemies
                .Select(CombatEncounterDraftEnemyDto.FromDomain)
                .ToArray(),
            Allies: draft.Allies
                .Select(CombatEncounterDraftAllyDto.FromDomain)
                .ToArray());
    }
}
