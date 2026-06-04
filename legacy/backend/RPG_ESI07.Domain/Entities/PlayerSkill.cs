namespace RPG_ESI07.Domain.Entities;

public class PlayerSkill
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int SkillId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PlayerProfile Player { get; set; } = null!;

    public Skill Skill { get; set; } = null!;
}