namespace RPG_ESI07.Domain.Entities;

public class CompanionState
{
    public int Id { get; set; }
    public string CurrentState { get; set; } = "REPOS";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int VictoriesToday { get; set; }
    public int DefeatesToday { get; set; }
    public DateTime LastCombatAt { get; set; } = DateTime.UtcNow;
}