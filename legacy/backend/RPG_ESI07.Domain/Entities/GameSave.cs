namespace RPG_ESI07.Domain.Entities;

public class GameSave
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string CurrentZone { get; set; } = string.Empty;
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public string? InventoryData { get; set; } // JSON
    public string? QuestFlags { get; set; } // JSON
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PlayerProfile Player { get; set; } = null!;
}