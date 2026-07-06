namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class NpcDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string CompatibleRoomTypesJson { get; set; } = "[]";
    public string CompatiblePalaceRoomStatesJson { get; set; } = "[]";
    public string CompatibleRoomClimatesJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";

    // ── NPC system (npc-system-sfd-0.1) ──────────────────────────────────────
    public string EmotionalAffinity { get; set; } = "Neutral";
    public bool IsRecurring { get; set; }
    public string? PersonaJson { get; set; }
    public string WoundsJson { get; set; } = "[]";
    public string? DialogueGraphJson { get; set; }
    public string EncounterKeysJson { get; set; } = "[]";

    // ── PNJ/Réputation refonte (npc-reputation-sfd-0.1) ──────────────────────
    public string BoundRoomKeysJson { get; set; } = "[]";
    public string? OfferingsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}