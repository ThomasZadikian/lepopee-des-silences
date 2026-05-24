namespace RPG_ESI07.Domain.Entities;

public class Npc
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = Constants.NpcTypeNeutral; // neutral, merchant, quest, ally
    public string? Description { get; set; }
    public string Zone { get; set; } = string.Empty;

    // ── Position sur la carte ─────────────────────────────────────────────────
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }

    // ── Système Markov ────────────────────────────────────────────────────────
    /// <summary>Rayon d'influence en unités Unity.</summary>
    public float InfluenceRadius { get; set; } = 5.0f;

    /// <summary>
    /// Matrice de transition Markov — JSON.
    /// Format : { "SEREIN": { "APEURÉ": 0.1, "SEREIN": 0.9 }, ... }
    /// </summary>
    public string? TransitionMatrix { get; set; }

    /// <summary>
    /// États disponibles sur la carte — JSON.
    /// Format : { "SEREIN": { "speed": 1, "detectionRange": 3 }, ... }
    /// </summary>
    public string? MapStates { get; set; }

    /// <summary>État initial au spawn.</summary>
    public string InitialState { get; set; } = "SEREIN";

    // ── Dialogues ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Dialogues par état — JSON.
    /// Format : { "SEREIN": ["Bonjour aventurier !"], "APEURÉ": ["Au secours !"] }
    /// </summary>
    public string? Dialogues { get; set; }

    // ── Marchand (optionnel) ──────────────────────────────────────────────────
    public bool IsMerchant { get; set; } = false;

    /// <summary>
    /// Inventaire marchand — JSON.
    /// Format : [{ "itemId": 1, "price": 50, "stock": 10 }]
    /// </summary>
    public string? MerchantInventory { get; set; }

    // ── Quêtes (optionnel) ────────────────────────────────────────────────────
    /// <summary>
    /// Quêtes associées — JSON.
    /// Format : [{ "questId": 1, "triggerState": "SEREIN" }]
    /// </summary>
    public string? Quests { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────────
    public ICollection<NpcInteraction> Interactions { get; set; } = new List<NpcInteraction>();
}