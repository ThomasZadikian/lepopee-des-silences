namespace RPG_ESI07.Domain.Entities;

public class NpcInteraction
{
    public int Id { get; set; }
    public int NpcId { get; set; }
    public int PlayerId { get; set; }
    public string EventType { get; set; } = string.Empty; // DIALOGUE, TRADE, QUEST_START
    public DateTime InteractedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Npc Npc { get; set; } = null!;
    public PlayerProfile Player { get; set; } = null!;
}