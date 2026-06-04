namespace RPG_ESI07.Domain.Entities;

public class PlayerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int CurrentHP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public int CurrentMP { get; set; } = 50;
    public int MaxMP { get; set; } = 50;
    public int Strength { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Speed { get; set; } = 10;
    public int Experience { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Progression ───────────────────────────────────────────────────────────
    /// <summary>Zone actuelle du joueur — pour le scaling des ennemis.</summary>
    public string CurrentZone { get; set; } = string.Empty;

    /// <summary>Multiplicateur de scaling global — calculé depuis le niveau.</summary>
    public float ScalingFactor { get; set; } = 1.0f;

    // ── Navigation properties ─────────────────────────────────────────────────
    public User User { get; set; } = null!;
    public ICollection<GameSave> GameSaves { get; set; } = new List<GameSave>();
    public CombatStats? CombatStats { get; set; }
    public ICollection<BestiaryUnlock> BestiaryUnlocks { get; set; } = new List<BestiaryUnlock>();
    public ICollection<PlayerInventory> Inventory { get; set; } = new List<PlayerInventory>();
    public ICollection<PlayerSkill> Skills { get; set; } = new List<PlayerSkill>();
    public ICollection<NpcInteraction> NpcInteractions { get; set; } = new List<NpcInteraction>();
}