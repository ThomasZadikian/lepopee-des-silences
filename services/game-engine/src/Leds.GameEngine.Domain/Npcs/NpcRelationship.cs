namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// Per-NPC runtime state carried by a Run. Lightweight by design (decision Q24a):
/// a relationship score, per-wound fracture states, a small set of typed flags,
/// the number of meetings, and the current dialogue node (so an encounter can span
/// several requests). The state machine logic lives here but stays primitive-typed
/// (no Catalog dependency); the resolver feeds it the authored thresholds.
/// </summary>
public sealed class NpcRelationship
{
    private readonly Dictionary<string, WoundState> _woundStates;
    private readonly HashSet<string> _flags;

    private NpcRelationship(
        string npcKey,
        int relationshipScore,
        Dictionary<string, WoundState> woundStates,
        HashSet<string> flags,
        int timesMet,
        string? currentDialogueNodeKey)
    {
        NpcKey = npcKey;
        RelationshipScore = relationshipScore;
        _woundStates = woundStates;
        _flags = flags;
        TimesMet = timesMet;
        CurrentDialogueNodeKey = currentDialogueNodeKey;
    }

    public string NpcKey { get; }

    public int RelationshipScore { get; private set; }

    public IReadOnlyDictionary<string, WoundState> WoundStates => _woundStates;

    public IReadOnlyCollection<string> Flags => _flags;

    public int TimesMet { get; private set; }

    public string? CurrentDialogueNodeKey { get; private set; }

    /// <summary>Aggregated NPC state = the most severe wound (decision §7).</summary>
    public WoundState AggregateState =>
        _woundStates.Count == 0 ? WoundState.Latent : _woundStates.Values.Max();

    public static NpcRelationship Begin(string npcKey, string? entryNodeKey)
    {
        return new NpcRelationship(
            npcKey,
            relationshipScore: 0,
            new Dictionary<string, WoundState>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            timesMet: 1,
            entryNodeKey);
    }

    public static NpcRelationship Rehydrate(
        string npcKey,
        int relationshipScore,
        IReadOnlyDictionary<string, WoundState> woundStates,
        IEnumerable<string> flags,
        int timesMet,
        string? currentDialogueNodeKey)
    {
        return new NpcRelationship(
            npcKey,
            relationshipScore,
            new Dictionary<string, WoundState>(woundStates, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(flags, StringComparer.OrdinalIgnoreCase),
            timesMet,
            currentDialogueNodeKey);
    }

    public void AdjustScore(int delta) => RelationshipScore += delta;

    public void SetFlag(string flag)
    {
        if (!string.IsNullOrWhiteSpace(flag))
        {
            _flags.Add(flag);
        }
    }

    public bool HasFlag(string flag) => _flags.Contains(flag);

    public void AdvanceTo(string? nodeKey) => CurrentDialogueNodeKey = nodeKey;

    public void RegisterNewMeeting()
    {
        TimesMet++;
    }

    public WoundState GetWoundState(string woundKey) =>
        _woundStates.TryGetValue(woundKey, out var state) ? state : WoundState.Latent;

    /// <summary>
    /// Escalation is always allowed; de-escalation only when the wound is revertible
    /// (maps authored reversibility — irreversible wounds stay ruptured for the run).
    /// </summary>
    public void SetWoundState(string woundKey, WoundState state, bool canRevert)
    {
        var current = GetWoundState(woundKey);
        if (state >= current || canRevert)
        {
            _woundStates[woundKey] = state;
        }
    }

    /// <summary>Recompute a wound's state from the current score and its thresholds.</summary>
    public void RefreshFromScore(string woundKey, int tenseThreshold, int ruptureThreshold, bool canRevert)
    {
        var computed = FractureEvaluator.Evaluate(RelationshipScore, tenseThreshold, ruptureThreshold);
        SetWoundState(woundKey, computed, canRevert);
    }
}