namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatantBaseStatSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid CombatantId { get; set; }
    public int MaxVitality { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public int StartingGuard { get; set; }
    public int Speed { get; set; }
    public int Initiative { get; set; }
    public int Recovery { get; set; }
    public int Focus { get; set; }
    public int Mana { get; set; }
    public int Charge { get; set; }
    public int MagicAttack { get; set; }
    public int MagicDefense { get; set; }
    public int Movement { get; set; } = 4;
    public int? AtbReadyThreshold { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public CombatantEntity? Combatant { get; set; }
}
