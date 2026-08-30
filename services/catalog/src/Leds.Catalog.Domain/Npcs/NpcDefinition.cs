using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.Domain.Npcs;

public sealed class NpcDefinition : CatalogContentBase, INpcDefinition
{
    private readonly List<string> _tags;
    private readonly List<string> _compatibleRoomTypes;
    private readonly List<string> _compatiblePalaceRoomStates;
    private readonly List<string> _compatibleRoomClimates;
    private readonly List<NpcWound> _wounds;
    private readonly List<string> _encounterKeys;
    private readonly List<string> _boundRoomKeys;
    private readonly List<NpcOffering> _offerings;

    private NpcDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> compatibleRoomTypes,
        IReadOnlyCollection<string> compatiblePalaceRoomStates,
        IReadOnlyCollection<string> compatibleRoomClimates,
        int? minDepth,
        int? maxDepth,
        EmotionalRegister emotionalAffinity,
        NpcPersona? persona,
        NpcDialogueGraph? dialogueGraph,
        IReadOnlyCollection<NpcWound> wounds,
        IReadOnlyCollection<string> encounterKeys,
        bool isRecurring,
        IReadOnlyCollection<string> boundRoomKeys,
        IReadOnlyCollection<NpcOffering> offerings)
        : base(id, key, name, description, version, status)
    {
        _tags = tags.ToList();
        _compatibleRoomTypes = compatibleRoomTypes.ToList();
        _compatiblePalaceRoomStates = compatiblePalaceRoomStates.ToList();
        _compatibleRoomClimates = compatibleRoomClimates.ToList();
        _wounds = wounds.ToList();
        _encounterKeys = encounterKeys.ToList();
        _boundRoomKeys = boundRoomKeys.ToList();
        _offerings = offerings.ToList();
        MinDepth = minDepth;
        MaxDepth = maxDepth;
        EmotionalAffinity = emotionalAffinity;
        Persona = persona;
        DialogueGraph = dialogueGraph;
        IsRecurring = isRecurring;
    }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<string> CompatibleRoomTypes => _compatibleRoomTypes.AsReadOnly();

    public IReadOnlyCollection<string> CompatiblePalaceRoomStates => _compatiblePalaceRoomStates.AsReadOnly();

    public IReadOnlyCollection<string> CompatibleRoomClimates => _compatibleRoomClimates.AsReadOnly();

    public int? MinDepth { get; }

    public int? MaxDepth { get; }

    public EmotionalRegister EmotionalAffinity { get; }

    public NpcPersona? Persona { get; }

    public NpcDialogueGraph? DialogueGraph { get; }

    public IReadOnlyCollection<NpcWound> Wounds => _wounds.AsReadOnly();

    public IReadOnlyCollection<string> EncounterKeys => _encounterKeys.AsReadOnly();

    public bool IsRecurring { get; }

    public IReadOnlyCollection<string> BoundRoomKeys => _boundRoomKeys.AsReadOnly();

    public IReadOnlyCollection<NpcOffering> Offerings => _offerings.AsReadOnly();

    public static NpcDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        IReadOnlyCollection<string>? tags,
        IReadOnlyCollection<string>? compatibleRoomTypes,
        IReadOnlyCollection<string>? compatiblePalaceRoomStates,
        IReadOnlyCollection<string>? compatibleRoomClimates,
        int? minDepth = null,
        int? maxDepth = null,
        CatalogContentStatus status = CatalogContentStatus.Draft,
        EmotionalRegister emotionalAffinity = EmotionalRegister.Neutral,
        NpcPersona? persona = null,
        NpcDialogueGraph? dialogueGraph = null,
        IReadOnlyCollection<NpcWound>? wounds = null,
        IReadOnlyCollection<string>? encounterKeys = null,
        bool isRecurring = false,
        IReadOnlyCollection<string>? boundRoomKeys = null,
        IReadOnlyCollection<NpcOffering>? offerings = null)
    {
        var desc = CatalogContentDescription.From(description);

        return new NpcDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            desc,
            CatalogContentVersion.From(version),
            status,
            tags?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatibleRoomTypes?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatiblePalaceRoomStates?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            compatibleRoomClimates?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            minDepth,
            maxDepth,
            emotionalAffinity,
            persona,
            dialogueGraph,
            wounds?.ToArray() ?? [],
            encounterKeys?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            isRecurring,
            boundRoomKeys?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            offerings?.ToArray() ?? []);
    }
}