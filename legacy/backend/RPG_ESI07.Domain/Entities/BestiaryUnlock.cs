namespace RPG_ESI07.Domain.Entities;

public class BestiaryUnlock
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int EnemyId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PlayerProfile Player { get; set; } = null!;

    public Enemy Enemy { get; set; } = null!;
}