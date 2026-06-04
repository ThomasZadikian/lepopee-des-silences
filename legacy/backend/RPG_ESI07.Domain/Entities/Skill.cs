namespace RPG_ESI07.Domain.Entities;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MPCost { get; set; }
    public int? BaseDamage { get; set; }
    public int? HealAmount { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public string? ElementType { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<PlayerSkill> PlayerSkills { get; set; } = new List<PlayerSkill>();
}