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
    private readonly Dictionary<RelationshipAxis, int> _axisScores;
    private readonly List<NpcMemoryEntry> _memories;

    private NpcRelationship(
        string npcKey,
        int relationshipScore,
        Dictionary<string, WoundState> woundStates,
        HashSet<string> flags,
        int timesMet,
        string? currentDialogueNodeKey,
        Dictionary<RelationshipAxis, int> axisScores,
        List<NpcMemoryEntry> memories)
    {
        NpcKey = npcKey;
        RelationshipScore = relationshipScore;
        _woundStates = woundStates;
        _flags = flags;
        TimesMet = timesMet;
        CurrentDialogueNodeKey = currentDialogueNodeKey;
        _axisScores = axisScores;
        _memories = memories;
    }

    public string NpcKey { get; }

    public int RelationshipScore { get; private set; }

    public IReadOnlyDictionary<string, WoundState> WoundStates => _woundStates;

    public IReadOnlyCollection<string> Flags => _flags;

    public int TimesMet { get; private set; }

    public string? CurrentDialogueNodeKey { get; private set; }

    /// <summary>Every axis this relationship has a non-zero score on. Axes never explicitly
    /// adjusted read as 0 via <see cref="GetAxisScore"/> without needing an entry here.</summary>
    public IReadOnlyDictionary<RelationshipAxis, int> AxisScores => _axisScores;

    /// <summary>What this specific NPC remembers — see <see cref="NpcMemoryEntry"/>.</summary>
    public IReadOnlyCollection<NpcMemoryEntry> Memories => _memories;

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
            entryNodeKey,
            new Dictionary<RelationshipAxis, int>(),
            new List<NpcMemoryEntry>());
    }

    public static NpcRelationship Rehydrate(
        string npcKey,
        int relationshipScore,
        IReadOnlyDictionary<string, WoundState> woundStates,
        IEnumerable<string> flags,
        int timesMet,
        string? currentDialogueNodeKey,
        IReadOnlyDictionary<RelationshipAxis, int>? axisScores = null,
        IEnumerable<NpcMemoryEntry>? memories = null)
    {
        return new NpcRelationship(
            npcKey,
            relationshipScore,
            new Dictionary<string, WoundState>(woundStates, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(flags, StringComparer.OrdinalIgnoreCase),
            timesMet,
            currentDialogueNodeKey,
            axisScores is null ? new Dictionary<RelationshipAxis, int>() : new Dictionary<RelationshipAxis, int>(axisScores),
            memories?.ToList() ?? new List<NpcMemoryEntry>());
    }

    // Elise's reputation can never decrease — she is "totalement apathique" and has no
    // wound/trigger of her own; any negative delta reaching her (e.g. a future authored
    // choice) is simply ignored rather than lowering her score. Applies to the secondary axes
    // too: her disposition doesn't move on any dimension.
    private const string NeverDecreasingNpcKey = "npc.elise";

    private bool IsExemptFromDecrease(int delta) =>
        delta < 0 && string.Equals(NpcKey, NeverDecreasingNpcKey, StringComparison.OrdinalIgnoreCase);

    public void AdjustScore(int delta)
    {
        if (IsExemptFromDecrease(delta))
        {
            return;
        }

        RelationshipScore += delta;
    }

    public int GetAxisScore(RelationshipAxis axis) => _axisScores.TryGetValue(axis, out var score) ? score : 0;

    public void AdjustAxisScore(RelationshipAxis axis, int delta)
    {
        if (IsExemptFromDecrease(delta))
        {
            return;
        }

        _axisScores[axis] = GetAxisScore(axis) + delta;
    }

    /// <summary>Records something this NPC now remembers. Multiple entries for the same
    /// knowledge key are allowed — e.g. the NPC first observed one thing and was later told a
    /// contradicting version — content-authoring decides whether that's meaningful.</summary>
    public void Remember(NpcMemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _memories.Add(entry);
    }

    /// <summary>Prunes every memory of the given scope — call at the matching lifecycle boundary
    /// (conversation end, room exit, run end; see <see cref="MemoryScope"/>'s own remarks).</summary>
    public void ForgetScope(MemoryScope scope) => _memories.RemoveAll(m => m.Scope == scope);

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