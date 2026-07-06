using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcDefinition(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    IReadOnlyCollection<PalaceRoomState> CompatiblePalaceRoomStates,
    IReadOnlyCollection<string> CompatibleRoomClimates,
    int MinDepth = 0,
    int MaxDepth = int.MaxValue,
    string EmotionalAffinity = "Neutral",
    bool IsRecurring = false,
    CatalogNpcPersona? Persona = null,
    CatalogNpcDialogueGraph? DialogueGraph = null,
    IReadOnlyCollection<CatalogNpcWound>? Wounds = null,
    IReadOnlyCollection<string>? EncounterKeys = null,
    IReadOnlyCollection<string>? BoundRoomKeys = null,
    IReadOnlyCollection<CatalogNpcOffering>? Offerings = null);