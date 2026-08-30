namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class EnemyDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string? Family { get; set; }
    public string Rank { get; set; } = "Common";
    public string? Role { get; set; }
    // Bestiaire authoring: 4-tier rarity (Common/Uncommon/Rare/MiniBoss), distinct from
    // the pre-existing Rank (Common/Elite/Boss) which the encounter-budget algorithm
    // already reads — Rarity is purely descriptive/authoring metadata.
    public string Rarity { get; set; } = "Common";
    // Bestiaire authoring: emotional register, mirrors NPCs' EmotionalRegister (as a
    // string, same convention as other free-text catalog fields on this entity).
    public string? Registre { get; set; }
    // Bestiaire authoring: encounter threat-budget weight (1-10), used to size
    // encounter compositions — distinct from BaseDifficulty (vitality/power scaling).
    public int MenaceLevel { get; set; }
    // Bestiaire authoring: precise room-key binding (mirrors NPC BoundRoomKeys),
    // additive to the existing coarse CompatibleRoomTypesJson category match — when
    // non-empty, encounter selection should prefer/require these specific rooms.
    public string BoundRoomKeysJson { get; set; } = "[]";
    public int BaseDifficulty { get; set; }
    public int EncounterWeight { get; set; } = 1;
    public int MinRiskLevel { get; set; }
    public int MaxRiskLevel { get; set; }
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public bool IsBoss { get; set; }
    public bool IsElite { get; set; }
    public string? RewardProfileKey { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string CompatibleRoomTypesJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";
    public string SkillKeysJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EnemyStatBlockEntity? StatBlock { get; set; }
    public ICollection<EnemySkillLinkEntity> SkillLinks { get; set; } = [];
    public ICollection<EnemyTagEntity> Tags { get; set; } = [];
}
