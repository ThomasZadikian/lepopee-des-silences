namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatantEntity
{
    public Guid Id { get; set; }
    public Guid CombatId { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public int MaxVitality { get; set; }
    public int CurrentVitality { get; set; }
    public int Guard { get; set; }
    public int BaseGuard { get; set; }
    public int Mana { get; set; }
    public int Charge { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional emotional attack type override (int value of EmotionalType), set
    /// from an AttackTypeOverride run modifier at combat creation. Null = no override.
    /// </summary>
    public int? AttackTypeOverride { get; set; }

    /// <summary>
    /// Equipment-driven typed damage reductions (EmotionalType int -> percent 0-100),
    /// serialized as JSON. Set at combat creation from the wearer's equipped items.
    /// </summary>
    public string? TypedDamageReductionsJson { get; set; }

    /// <summary>
    /// Equipment-driven hit chance bonus (percentage points) and DamageOverTime
    /// duration/damage reductions. Set at combat creation from the wearer's
    /// equipped items (e.g. Lunettes d'érudit, Main de Khasma).
    /// </summary>
    public int HitChanceBonusPercent { get; set; }
    public int DotDurationReductionPercent { get; set; }
    public int DotDamageReductionPercent { get; set; }

    /// <summary>
    /// Equipment-driven Magic-category damage bonus/reduction (percentage points).
    /// Set at combat creation from the wearer's equipped items (e.g. Pomenian's monocle).
    /// </summary>
    public int MagicDamageBonusPercent { get; set; }
    public int MagicDamageReductionPercent { get; set; }

    /// <summary>
    /// Active durable status effects (poison/regen/buffs/control) serialized as JSON.
    /// In-combat only and small, so a JSON column avoids a child table and is copied
    /// automatically by the hot-path persistence (a scalar via CurrentValues.SetValues).
    /// </summary>
    public string? StatusEffectsJson { get; set; }

    public CombatEntity? Combat { get; set; }
    public List<CombatantSkillEntity> Skills { get; set; } = [];
    public CombatantBaseStatSnapshotEntity? BaseStatSnapshot { get; set; }
    public CombatantRuntimeStateEntity? RuntimeState { get; set; }
}