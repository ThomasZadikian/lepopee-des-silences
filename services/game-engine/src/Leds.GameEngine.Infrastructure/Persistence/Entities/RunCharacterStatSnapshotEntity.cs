namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunCharacterStatSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public int MaxVitality { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public int StartingGuard { get; set; }
    public int Speed { get; set; }
    public int Initiative { get; set; }
    public int Focus { get; set; }
    public int Mana { get; set; }
    public int Charge { get; set; }
    public int MagicAttack { get; set; }
    public int MagicDefense { get; set; }

    public RunCharacterSnapshotEntity? CharacterSnapshot { get; set; }
}
