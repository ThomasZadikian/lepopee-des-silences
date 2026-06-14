using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public sealed record CombatEncounterDraftContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    int EnemyCount,
    int NodeDepth = 0,
    IReadOnlyCollection<ActivePalaceLaw>? ActivePalaceLaws = null);