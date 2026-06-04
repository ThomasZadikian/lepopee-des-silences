namespace RPG_ESI07.Domain.Entities;

public class PlayerInventory
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsEquipped { get; set; } = false;

    // Navigation properties
    public PlayerProfile Player { get; set; } = null!;

    public Item Item { get; set; } = null!;
}