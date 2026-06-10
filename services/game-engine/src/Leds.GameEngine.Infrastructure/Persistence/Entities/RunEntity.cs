namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Seed { get; set; } = string.Empty;
    public string GeneratorVersion { get; set; } = string.Empty;
    public string MarkovMatrixVersion { get; set; } = string.Empty;
    public int CurrentDepth { get; set; }
    public Guid? ActiveCombatId { get; set; }
    public Guid? PendingRewardOfferId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
