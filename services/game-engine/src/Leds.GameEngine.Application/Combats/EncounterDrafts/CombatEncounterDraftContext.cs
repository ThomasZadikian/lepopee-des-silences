namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraftContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    int EnemyCount);
