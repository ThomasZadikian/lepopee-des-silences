namespace RPG_ESI07.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; } 
    public string? StatModifiers { get; set; }
    public int? EffectValue { get; set; }
    public string? Description { get; set; }
    public int Price { get; set; } = 0;

    // Navigation properties
    public ICollection<PlayerInventory> PlayerInventories { get; set; } = new List<PlayerInventory>();
}