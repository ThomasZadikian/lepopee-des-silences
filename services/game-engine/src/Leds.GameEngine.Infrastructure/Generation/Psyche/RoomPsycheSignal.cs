using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Psyche;

/// <summary>
/// Tous les signaux d'une salle traversée qui poussent la psyché. Déterministe :
/// reconstruit depuis l'historique (run.Rooms + nodes résolus + modifiers de climat).
/// RoomType nullable + collections optionnelles → l'ancien Nudge(PalaceRoomState) reste valide.
/// </summary>
public sealed record RoomPsycheSignal(
    PalaceRoomState PalaceState,
    RoomType? RoomType = null,
    IReadOnlyCollection<NodeEventType>? ResolvedChoices = null,
    IReadOnlyCollection<string>? ChosenEventOptionIds = null,
    string? Climate = null);