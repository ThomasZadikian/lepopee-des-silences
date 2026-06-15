namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunItemEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string DefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public int EffectAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
}