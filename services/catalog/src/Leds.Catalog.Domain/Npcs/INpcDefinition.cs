using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Npcs;

public interface INpcDefinition : ICatalogContent
{
    IReadOnlyCollection<string> Tags { get; }

    IReadOnlyCollection<string> CompatibleRoomTypes { get; }

    IReadOnlyCollection<string> CompatiblePalaceRoomStates { get; }

    IReadOnlyCollection<string> CompatibleRoomClimates { get; }

    int? MinDepth { get; }

    int? MaxDepth { get; }

    EmotionalRegister EmotionalAffinity { get; }

    NpcPersona? Persona { get; }

    NpcDialogueGraph? DialogueGraph { get; }

    IReadOnlyCollection<NpcWound> Wounds { get; }

    IReadOnlyCollection<string> EncounterKeys { get; }

    bool IsRecurring { get; }

    IReadOnlyCollection<string> BoundRoomKeys { get; }

    IReadOnlyCollection<NpcOffering> Offerings { get; }
}