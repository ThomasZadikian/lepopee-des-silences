namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid RoomId { get; set; }
    public Guid NodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public Guid? ActiveCombatantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
    public List<CombatantEntity> Combatants { get; set; } = [];
}
