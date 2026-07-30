namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class SkillDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty;
    public string TargetingType { get; set; } = string.Empty;
    public string TargetingMode { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    // Physical|Magic — determines eligibility for category-scoped combat bonuses
    // (e.g. Pomenian's "Connaissance académique": +Magic damage / -incoming Magic damage).
    public string Category { get; set; } = "Physical";
    public string CostType { get; set; } = "None";
    public int CostAmount { get; set; }
    public int ManaCost { get; set; }
    public int ChargeCost { get; set; }
    public int BasePower { get; set; }
    // When true, BasePower is a percentage of the target's MaxVitality (instant heal),
    // not a flat amount — e.g. Mané's "Favorite de Elise" (+15% PV instantly).
    public bool BasePowerIsPercentOfMaxVitality { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; } = 100;
    public int ActionCost { get; set; } = 10;
    public int CastTime { get; set; }
    public int RecoveryTime { get; set; }
    public int Cooldown { get; set; }
    public int TacticalRange { get; set; } = 1;
    public string TacticalAreaShape { get; set; } = "Single";
    public bool RequiresLineOfSight { get; set; }
    public bool IsUltimate { get; set; }
    public string EmotionalRegister { get; set; } = "Neutral";
    public Guid? EffectSetId { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }

    // Durable effects this skill applies to its targets (empty/null = none). A skill
    // may carry several simultaneously (e.g. heal-over-time + guard-over-time).
    public string? EffectsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<SkillTagEntity> Tags { get; set; } = [];
}
