namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class CatalogTagEntity
{
    public Guid Id { get; set; }
    public string TagKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class EnemyStatBlockEntity
{
    public Guid Id { get; set; }
    public Guid EnemyDefinitionId { get; set; }
    public int MaxVitality { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public int StartingGuard { get; set; }
    public int Speed { get; set; }
    public int Initiative { get; set; }
    public int Recovery { get; set; }
    public int Focus { get; set; }
    public int Mana { get; set; }
    public int Charge { get; set; }
    public int MagicAttack { get; set; }
    public int MagicDefense { get; set; }
    public int Movement { get; set; } = 4;

    public EnemyDefinitionEntity EnemyDefinition { get; set; } = null!;
}

public sealed class EnemySkillLinkEntity
{
    public Guid EnemyDefinitionId { get; set; }
    public string SkillDefinitionKey { get; set; } = string.Empty;

    public EnemyDefinitionEntity EnemyDefinition { get; set; } = null!;
}

public sealed class EnemyTagEntity
{
    public Guid EnemyDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public EnemyDefinitionEntity EnemyDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class SkillTagEntity
{
    public Guid SkillDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public SkillDefinitionEntity SkillDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class ItemTagEntity
{
    public Guid ItemDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public ItemDefinitionEntity ItemDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class LawTagEntity
{
    public Guid PalaceLawDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public PalaceLawDefinitionEntity PalaceLawDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class CurseTagEntity
{
    public Guid CurseDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public CurseDefinitionEntity CurseDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class RewardTemplateTagEntity
{
    public Guid RewardTemplateId { get; set; }
    public Guid TagId { get; set; }

    public RewardTemplateEntity RewardTemplate { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class RoomBossTagEntity
{
    public Guid RoomBossDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public RoomBossDefinitionEntity RoomBossDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}

public sealed class EffectSetEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<EffectDefinitionEntity> Effects { get; set; } = [];
}

public sealed class EffectDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid EffectSetId { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public string TargetScope { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string ValueMode { get; set; } = "Flat";
    public string Duration { get; set; } = "Immediate";
    public string StackPolicy { get; set; } = "None";
    public string? Condition { get; set; }
    public int Order { get; set; }
    public string? BehaviorTag { get; set; }
    public string? GenerationTag { get; set; }
    public string? SelectionGroup { get; set; }

    public EffectSetEntity EffectSet { get; set; } = null!;
}

public sealed class CurseDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public int Severity { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? Trigger { get; set; }
    public Guid? EffectSetId { get; set; }
    public int BaseWeight { get; set; } = 1;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string? SelectionGroup { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<CurseTagEntity> Tags { get; set; } = [];
}

public sealed class RewardTemplateEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int MinOptions { get; set; } = 1;
    public int MaxOptions { get; set; } = 3;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RewardTemplateOptionEntity> Options { get; set; } = [];
    public ICollection<RewardTemplateTagEntity> Tags { get; set; } = [];
}

public sealed class RewardTemplateOptionEntity
{
    public Guid Id { get; set; }
    public Guid RewardTemplateId { get; set; }
    public string RewardType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PayloadKey { get; set; }
    public int? BaseAmount { get; set; }
    public string ScalingMode { get; set; } = "Flat";
    public int Weight { get; set; } = 1;
    public Guid? EffectSetId { get; set; }

    /// <summary>
    /// Only meaningful for RewardType "TemporaryItem" options — mirrors the game-engine
    /// RunItemType/RunItemRarity/RunItemEffectType enums by name (e.g. "Consumable",
    /// "Uncommon", "Guard"), so RewardOfferFactory can build a full
    /// "item:&lt;key&gt;:&lt;label&gt;:&lt;desc&gt;:&lt;type&gt;:&lt;rarity&gt;:&lt;effectType&gt;:&lt;amount&gt;"
    /// reward payload (see Run.AddRunItemFromPayload) instead of hardcoding
    /// Consumable/Common/Heal for every templated item option.
    /// </summary>
    public string? ItemType { get; set; }
    public string? ItemRarity { get; set; }
    public string? ItemEffectType { get; set; }

    public RewardTemplateEntity RewardTemplate { get; set; } = null!;
    public EffectSetEntity? EffectSet { get; set; }
}

public sealed class RoomDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string RoomFamily { get; set; } = string.Empty;
    public string RoomRarity { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string? EnemyPoolKey { get; set; }
    public string? RewardPoolKey { get; set; }
    public string? LawPoolKey { get; set; }
    public string? CursePoolKey { get; set; }
    public string? SpecialMechanicKey { get; set; }
    public string? BossDefinitionKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsCulturalEcho { get; set; }
    public Guid? WorldDefinitionId { get; set; }
    public string ReachabilityMode { get; set; } = "Explicit";
    public bool TriggersStrictChain { get; set; }
    /// <summary>
    /// True for rooms that must only ever be reached through an explicit strict-chain
    /// edge (e.g. the Enfers floors, the Château cell). Excluded from any "AllExceptListed"
    /// deny-list resolution even though they belong to the same World, so a connector room
    /// like room.couloirs can never accidentally route into the middle of a chain.
    /// </summary>
    public bool ExcludeFromOpenPool { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public WorldDefinitionEntity? WorldDefinition { get; set; }
    public ICollection<RoomTagEntity> Tags { get; set; } = [];
    public ICollection<RoomReachabilityEntity> ReachableTo { get; set; } = [];
    public ICollection<RoomReachabilityEntity> ReachableFrom { get; set; } = [];
}

/// <summary>
/// A grouping of rooms sharing a narrative universe (e.g. "Palais"). Each World declares
/// exactly one entry/fallback room, used both as the return point after a strict chain
/// ends and as the fallback when reachability generation finds no eligible candidate.
/// </summary>
public sealed class WorldDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid EntryRoomDefinitionId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public RoomDefinitionEntity EntryRoomDefinition { get; set; } = null!;

    /// <summary>
    /// Every room assigned to this World. Declared explicitly (rather than left as a bare
    /// WithMany()) so EF Core can never conflate this one-to-many relationship with the
    /// unrelated World -&gt; EntryRoomDefinition reference above — without an explicit
    /// collection on both sides, EF's navigation-pairing convention has been observed to
    /// merge two distinct FK relationships between the same two entity types into one,
    /// incorrectly marking RoomDefinitionEntity.WorldDefinitionId as unique.
    /// </summary>
    public ICollection<RoomDefinitionEntity> Rooms { get; set; } = [];
}

/// <summary>
/// An explicit reachability edge: FromRoomDefinition can transition directly to
/// ToRoomDefinition. When the source room's ReachabilityMode is "AllExceptListed",
/// these rows are interpreted as exclusions instead of inclusions and resolved into
/// an effective allow-list at read time (see EfRoomDefinitionReadStore).
/// </summary>
public sealed class RoomReachabilityEntity
{
    public Guid FromRoomDefinitionId { get; set; }
    public Guid ToRoomDefinitionId { get; set; }

    public RoomDefinitionEntity FromRoomDefinition { get; set; } = null!;
    public RoomDefinitionEntity ToRoomDefinition { get; set; } = null!;
}

/// <summary>
/// Sparse editorial override of the transition weight between two room themes. Any
/// theme pair without an explicit row uses the configured default weight, so adding a
/// new theme in content never requires a matching engine or matrix change.
/// </summary>
public sealed class RoomThemeAffinityEntity
{
    public Guid Id { get; set; }
    public string ThemeFrom { get; set; } = string.Empty;
    public string ThemeTo { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1.0m;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class NpcReputationAffinityEntity
{
    public Guid Id { get; set; }
    public string NpcKeyFrom { get; set; } = string.Empty;
    public string NpcKeyTo { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class RoomTypeDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string DefaultRarity { get; set; } = string.Empty;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class RoomSpecialMechanicEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MechanicType { get; set; } = string.Empty;
    public Guid? EffectSetId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public EffectSetEntity? EffectSet { get; set; }
}

public sealed class RoomBossDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string? EnemyDefinitionKey { get; set; }
    public string? DangerHint { get; set; }
    public int BaseDifficulty { get; set; } = 1;
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TagsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RoomBossTagEntity> Tags { get; set; } = [];
}

public sealed class RoomTagEntity
{
    public Guid RoomDefinitionId { get; set; }
    public Guid TagId { get; set; }

    public RoomDefinitionEntity RoomDefinition { get; set; } = null!;
    public CatalogTagEntity Tag { get; set; } = null!;
}
