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
    public int Mana { get; set; }
    public int Charge { get; set; }
    public string Status { get; set; } = string.Empty;

    public CombatEntity? Combat { get; set; }
    public List<CombatantSkillEntity> Skills { get; set; } = [];
}
