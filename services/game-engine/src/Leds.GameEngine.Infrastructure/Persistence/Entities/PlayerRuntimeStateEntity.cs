namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class PlayerRuntimeStateEntity
{
    public Guid RunId { get; set; }
    public int MaxVitality { get; set; }
    public int CurrentVitality { get; set; }
    public int Guard { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; } = int.MaxValue;
    public int Charge { get; set; }

    public RunEntity? Run { get; set; }
    public List<PlayerRuntimeSkillEntity> Skills { get; set; } = [];
}