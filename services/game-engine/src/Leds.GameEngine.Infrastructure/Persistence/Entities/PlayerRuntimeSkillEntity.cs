namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class PlayerRuntimeSkillEntity
{
    public Guid RunId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty;
    public string TargetingType { get; set; } = string.Empty;
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
    public string EmotionalRegister { get; set; } = string.Empty;

    public PlayerRuntimeStateEntity? PlayerState { get; set; }
}
