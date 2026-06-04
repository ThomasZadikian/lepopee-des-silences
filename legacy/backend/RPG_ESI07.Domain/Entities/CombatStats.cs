namespace RPG_ESI07.Domain.Entities;

public class CombatStats
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int TotalCombats { get; set; } = 0;
    public int CombatsWon { get; set; } = 0;
    public int CombatsLost { get; set; } = 0;
    public long TotalDamageDealt { get; set; } = 0;
    public long TotalDamageTaken { get; set; } = 0;
    public int TotalPlaytimeMinutes { get; set; } = 0;

    // Navigation properties
    public PlayerProfile Player { get; set; } = null!;
}