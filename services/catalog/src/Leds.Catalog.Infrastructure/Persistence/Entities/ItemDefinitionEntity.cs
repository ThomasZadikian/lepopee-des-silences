namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class ItemDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    // Closed vocabulary, ItemCategory (catalog) — the sole field the runtime maps to
    // RunItemType (CatalogRunItemMapper). Validated against ItemTypeCatalog at every
    // seed write, never parsed with a fallback.
    public string Category { get; set; } = string.Empty;
    // Free narrative subtype (e.g. "Lore", "Potion", "Trophée") — flavor text only,
    // never consulted by gameplay logic. Do not switch on this field.
    public string FlavorTag { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string UsageMode { get; set; } = "NotUsable";
    public string Lifecycle { get; set; } = "RuntimeRunOnly";
    public string StackPolicy { get; set; } = "Additive";
    public int MaxStack { get; set; } = 1;
    public bool IsUsableInCombat { get; set; }
    public bool IsUsableOutsideCombat { get; set; }
    public Guid? EffectSetId { get; set; }
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string Duration { get; set; } = string.Empty;
    public int EffectValue { get; set; }

    /// <summary>
    /// The RunItemEffectType (as a string, e.g. "Heal"/"Guard"/"ManaRestore") this
    /// item applies when used/granted directly as a consumable — distinct from
    /// EquipmentEffectsJson, which drives equipped-item passive bonuses. Null/"None"
    /// for items with no intrinsic instant effect (relics, equipment, etc.).
    /// </summary>
    public string? EffectRunType { get; set; }

    // Mandatory tactical contract for combat-usable items. Equipment can also
    // define the basic attack contract when it represents a weapon.
    public int TacticalRange { get; set; } = 1;
    public string TacticalAreaShape { get; set; } = "Single";
    public bool RequiresLineOfSight { get; set; }
    public int? BasicAttackPower { get; set; }
    public string? BasicAttackCategory { get; set; }

    public int Price { get; set; }

    // ── Équipement et sac permanent (equipment-sfd-0.1) ──────────────────────
    public string? EquipmentEffectsJson { get; set; }

    // ── Récipients et liquides ────────────────────────────────────────────
    public bool IsContainer { get; set; }
    public int? ContainerCapacity { get; set; }
    public bool IsLiquid { get; set; }

    // ── Objet lisible (carnet/journal) ────────────────────────────────────
    // JSON array of strings, one entry per page — e.g. ["PLACEHOLDER HISTOIRE 01"].
    // Empty/null = not readable (no "Lire" affordance on the frontend).
    public string? ReadablePagesJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<ItemTagEntity> Tags { get; set; } = [];
}
