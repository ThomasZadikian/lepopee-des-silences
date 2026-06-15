namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraft(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    IReadOnlyCollection<CombatEncounterDraftEnemy> Enemies,
    IReadOnlyCollection<CombatEncounterDraftAlly> Allies,
    double DifficultyMultiplier = 1.0);