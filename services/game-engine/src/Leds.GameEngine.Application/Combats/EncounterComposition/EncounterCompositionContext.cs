using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Combats.EncounterComposition;

public sealed record EncounterCompositionContext(
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    IReadOnlyCollection<CatalogEnemyDefinition> AvailableEnemies,
    int NodeDepth = 0,
    IReadOnlyCollection<ActivePalaceLaw>? ActivePalaceLaws = null,
    PalaceRoomState PalaceRoomState = PalaceRoomState.Neutral,
    string? RoomClimate = null);