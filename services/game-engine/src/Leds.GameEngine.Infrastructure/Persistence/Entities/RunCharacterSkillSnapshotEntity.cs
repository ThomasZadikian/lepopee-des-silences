namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunCharacterSkillSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public string SkillDefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty;
    public string TargetingMode { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public int ChargeCost { get; set; }
    public int BasePower { get; set; }
    public string Category { get; set; } = "Physical";
    public bool BasePowerIsPercentOfMaxVitality { get; set; }
    public int TacticalRange { get; set; } = 1;
    public string TacticalAreaShape { get; set; } = "Single";
    public bool RequiresLineOfSight { get; set; }
    public int Cooldown { get; set; }
    public bool IsUltimate { get; set; }
    public string EmotionalRegister { get; set; } = "Neutral";

    public RunCharacterSnapshotEntity? CharacterSnapshot { get; set; }
}
