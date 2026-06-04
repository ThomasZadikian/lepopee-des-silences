namespace RPG_ESI07.Domain.Entities;

public class Enemy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = Constants.EnemyTypeBasic;
    public int MaxHP { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Speed { get; set; }
    public float PhysicalResistance { get; set; } = 1.0f;
    public float MagicalResistance { get; set; } = 1.0f;
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public string? Description { get; set; }

    // ── Système Markov ────────────────────────────────────────────────────────
    /// <summary>
    /// Rayon d'influence en unités Unity.
    /// Boss = 10, standard = 5 (défaut).
    /// </summary>
    public float InfluenceRadius { get; set; } = 5.0f;

    /// <summary>
    /// Matrice de transition Markov — JSON.
    /// Format : { "AGRESSIF": { "DEFENSIF": 0.4, "PANIQUÉ": 0.3, "AGRESSIF": 0.3 }, ... }
    /// </summary>
    public string? TransitionMatrix { get; set; }

    /// <summary>
    /// Scripts par état — JSON.
    /// Format : { "AGRESSIF": { "actions": ["attack", "poison"], "damageMultiplier": 1.5 }, ... }
    /// </summary>
    public string? CombatScripts { get; set; }

    /// <summary>
    /// États disponibles sur la carte — JSON.
    /// Format : { "REPOS": { "speed": 0, "detectionRange": 0 }, "PATROUILLE": { ... } }
    /// </summary>
    public string? MapStates { get; set; }

    /// <summary>
    /// État initial au spawn.
    /// </summary>
    public string InitialState { get; set; } = "REPOS";

    // ── Navigation properties ─────────────────────────────────────────────────
    public ICollection<BestiaryUnlock> BestiaryUnlocks { get; set; } = new List<BestiaryUnlock>();
}