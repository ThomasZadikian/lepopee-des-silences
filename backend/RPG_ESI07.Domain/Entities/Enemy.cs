namespace RPG_ESI07.Domain.Entities;

public class Enemy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = Constants.EnemyTypeBasic; // basic, miniboss, boss
    public int MaxHP { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Speed { get; set; }
    public float PhysicalResistance { get; set; } = 1.0f; // 0.5=résiste, 1.0=normal, 2.0=faible
    public float MagicalResistance { get; set; } = 1.0f;
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<BestiaryUnlock> BestiaryUnlocks { get; set; } = new List<BestiaryUnlock>();
}