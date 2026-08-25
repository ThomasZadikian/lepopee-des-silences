using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Nodes;

public sealed class MapNode
{
    private readonly List<NodeId> _parentNodeIds;

    private MapNode(
        NodeId id,
        NodeEventType eventType,
        int row,
        int lane,
        int riskLevel,
        RiskTier? combatRiskTier,
        string rewardProfile,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isBoss,
        NodeState state,
        HiddenState hiddenState,
        DangerTell dangerTell,
        ContactBehavior contactBehavior,
        string? exitDestinationRoomKey,
        string? exitDestinationDisplayName)
    {
        HiddenState = hiddenState;
        DangerTell = dangerTell;
        ContactBehavior = contactBehavior;
        Id = id;
        EventType = eventType;
        Row = row;
        Lane = lane;
        RiskLevel = riskLevel;
        CombatRiskTier = combatRiskTier;
        RewardProfile = rewardProfile;
        _parentNodeIds = parentNodeIds.ToList();
        IsBoss = isBoss;
        State = state;
        ExitDestinationRoomKey = exitDestinationRoomKey;
        ExitDestinationDisplayName = exitDestinationDisplayName;
    }

    public NodeId Id { get; }

    public NodeEventType EventType { get; }

    public int Row { get; private set; }

    public int Lane { get; private set; }

    /// <summary>
    /// Raw generation roll (0-100). Kept as-is for non-combat nodes (Item/Merchant/etc.),
    /// which use it to vary reward generosity — a concern separate from combat danger.
    /// </summary>
    public int RiskLevel { get; }

    /// <summary>
    /// The node's combat danger tier — the sole difficulty axis for combat-flavored nodes
    /// (Combat/Elite/Rare/RoomBoss/FinalBoss). Null for every other node type: danger has
    /// no meaning for an Item/Npc/Memory/Rest/Merchant/Law/Curse node. Mutable only via
    /// <see cref="RaiseRisk"/> (the "provoquer le destin" wager).
    /// </summary>
    public RiskTier? CombatRiskTier { get; private set; }

    public string RewardProfile { get; }

    public IReadOnlyCollection<NodeId> ParentNodeIds => _parentNodeIds.AsReadOnly();

    public bool IsBoss { get; }

    public NodeState State { get; private set; }

    /// <summary>
    /// Whether this node still has to be found before it can be entered. Mutable only via
    /// <see cref="Reveal"/> (searching the ground around the party).
    /// </summary>
    public HiddenState HiddenState { get; private set; }

    /// <summary>The warning the node gives off before contact. See <see cref="DangerTell"/>.</summary>
    public DangerTell DangerTell { get; }

    /// <summary>What walking onto this node's cell does. See <see cref="ContactBehavior"/>.</summary>
    public ContactBehavior ContactBehavior { get; }

    /// <summary>
    /// The catalog room this exit leads to (<see cref="NodeEventType.Exit"/> only, otherwise
    /// always null). Null is itself a meaningful value on an Exit node: it marks a room with
    /// no reachability graph (legacy catalog content with no WorldKey) — the destination is
    /// resolved via the old per-theme weighted roll at confirmation time instead of a fixed key.
    /// </summary>
    public string? ExitDestinationRoomKey { get; }

    /// <summary>
    /// Cached display name for <see cref="ExitDestinationRoomKey"/>, set once at generation
    /// time so the node popup never needs a second catalog round-trip. A placeholder ("???")
    /// when the destination is not fixed (see <see cref="ExitDestinationRoomKey"/>).
    /// </summary>
    public string? ExitDestinationDisplayName { get; }

    /// <summary>
    /// A hidden node is not enterable and must not be advertised to the client until searched —
    /// the whole point being that the player does not know it is there.
    /// </summary>
    public bool IsHidden => HiddenState == HiddenState.Hint;

    /// <summary>Walking onto this cell resolves the node with no prompt.</summary>
    public bool TriggersOnContact =>
        IsCombatFlavored(EventType)
        || ContactBehavior is ContactBehavior.TriggerOnEnter or ContactBehavior.Blocking;

    /// <summary>The cell cannot be crossed in transit — a path may end on it, never pass through.</summary>
    public bool BlocksTransit =>
        IsCombatFlavored(EventType) || ContactBehavior == ContactBehavior.Blocking;

    /// <summary>
    /// A regular exploration encounter is represented by its combat node while it is waiting.
    /// Scripted combats created by a rule/event have no combat node and therefore never enter
    /// this actor loop.
    /// </summary>
    public bool CanRoamAsHostile =>
        IsCombatFlavored(EventType) && State == NodeState.Available && !IsHidden;

    public bool IsInitial => Row == 0 && _parentNodeIds.Count == 0 && !IsBoss;

    public bool IsAvailable => State == NodeState.Available;

    public bool IsPlanned => State == NodeState.Planned;

    public string? ChosenEventOptionId { get; private set; }

    public bool HasChosenEventOption => !string.IsNullOrWhiteSpace(ChosenEventOptionId);

    /// <summary>
    /// Combat-flavored node types are the only ones with a meaningful combat danger tier —
    /// mirrors <c>ICombatRiskProfileResolver.IsCombatNodeType</c> (kept in sync manually,
    /// since Domain must not depend on Application).
    /// </summary>
    public static bool IsCombatFlavored(NodeEventType eventType) =>
        eventType is NodeEventType.Combat
                  or NodeEventType.Rare
                  or NodeEventType.Elite
                  or NodeEventType.RoomBoss
                  or NodeEventType.FinalBoss;

    public static MapNode Create(
        NodeEventType eventType,
        int riskLevel,
        string rewardProfile,
        int row,
        int lane,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isBoss = false,
        NodeState initialState = NodeState.Available,
        RiskTier? combatRiskTier = null,
        HiddenState hiddenState = HiddenState.None,
        DangerTell dangerTell = DangerTell.None,
        ContactBehavior contactBehavior = ContactBehavior.None,
        string? exitDestinationRoomKey = null,
        string? exitDestinationDisplayName = null)
    {
        if (riskLevel is < 0 or > 100)
        {
            throw new DomainException("MapNode risk level must be between 0 and 100.");
        }

        if (combatRiskTier is not null && !IsCombatFlavored(eventType))
        {
            throw new DomainException($"Non-combat node type '{eventType}' must not have a CombatRiskTier.");
        }

        if (string.IsNullOrWhiteSpace(rewardProfile))
        {
            throw new DomainException("MapNode reward profile is required.");
        }

        if (row < 0)
        {
            throw new DomainException("MapNode row must be greater than or equal to 0.");
        }

        if (lane < 0)
        {
            throw new DomainException("MapNode lane must be greater than or equal to 0.");
        }

        if (initialState is not NodeState.Planned and not NodeState.Available)
        {
            throw new DomainException("A newly created MapNode must be Planned or Available.");
        }

        var parentList = parentNodeIds?.Distinct().ToList()
            ?? throw new DomainException("Parent node ids are required.");

        if (row == 0 && parentList.Count != 0)
        {
            throw new DomainException("Initial row MapNodes cannot have parents.");
        }

        if (isBoss && eventType != NodeEventType.RoomBoss && eventType != NodeEventType.FinalBoss)
        {
            throw new DomainException("A boss MapNode must have a RoomBoss or FinalBoss event type.");
        }

        // An authored boss encounter and an exit are discoverable landmarks, never search-only
        // caches. Bosses remain optional at room level.
        if ((isBoss || eventType == NodeEventType.Exit) && hiddenState != HiddenState.None)
        {
            throw new DomainException("A boss or exit MapNode cannot be hidden.");
        }

        if (eventType != NodeEventType.Exit
            && (exitDestinationRoomKey is not null || exitDestinationDisplayName is not null))
        {
            throw new DomainException($"Non-exit node type '{eventType}' must not carry an exit destination.");
        }

        // A node starts either plainly visible or waiting to be found; 'Revealed' is a state you
        // arrive at by searching, never one you are created in.
        if (hiddenState == HiddenState.Revealed)
        {
            throw new DomainException("A newly created MapNode cannot start already revealed.");
        }

        // A danger tell is the warning that a contact trigger is coming. On a node nothing
        // happens on contact it would be a lie told to the player.
        if (dangerTell != DangerTell.None && contactBehavior == ContactBehavior.None)
        {
            throw new DomainException("Only a contact-triggered MapNode can carry a danger tell.");
        }

        return new MapNode(
            NodeId.New(),
            eventType,
            row,
            lane,
            riskLevel,
            combatRiskTier,
            rewardProfile.Trim(),
            parentList,
            isBoss,
            initialState,
            hiddenState,
            dangerTell,
            contactBehavior,
            exitDestinationRoomKey,
            exitDestinationDisplayName);
    }

    public void AddParent(NodeId parentId)
    {
        if (_parentNodeIds.Contains(parentId))
        {
            return;
        }

        _parentNodeIds.Add(parentId);
    }

    public void Unlock()
    {
        if (State != NodeState.Planned)
        {
            throw new DomainException("Only a planned MapNode can be unlocked.");
        }

        State = NodeState.Available;
    }

    public void Select()
    {
        if (State != NodeState.Available)
        {
            throw new DomainException("Only an available MapNode can be selected.");
        }

        State = NodeState.Selected;
    }

    /// <summary>Moves the exploration representation of an available combat encounter by one
    /// orthogonal cell. Room owns collision/path validation; this method owns the node guard.</summary>
    public void MoveExplorationActorTo(int lane, int row)
    {
        if (!CanRoamAsHostile)
        {
            throw new DomainException("Only an available combat node can move in exploration.");
        }

        if (lane < 0 || row < 0 || Math.Abs(lane - Lane) + Math.Abs(row - Row) != 1)
        {
            throw new DomainException("A combat exploration actor must move by one orthogonal cell.");
        }

        Lane = lane;
        Row = row;
    }

    public void Lock()
    {
        if (State is NodeState.Selected or NodeState.Resolved or NodeState.Unreachable)
        {
            return;
        }

        if (State == NodeState.Planned)
        {
            return;
        }

        State = NodeState.Locked;
    }

    public void MarkUnreachable()
    {
        if (State is NodeState.Selected or NodeState.Resolved)
        {
            return;
        }

        State = NodeState.Unreachable;
    }

    public void Resolve()
    {
        if (State != NodeState.Selected)
        {
            throw new DomainException("Only a selected MapNode can be resolved.");
        }

        State = NodeState.Resolved;
    }

    /// <summary>
    /// "Provoquer le destin" — the player raises this node's combat danger by one tier
    /// in exchange for a better reward (see CombatRiskProfile's Loot/Reputation/Éclats
    /// multipliers). Repeatable: each call raises the tier by one step, capped at Fatal.
    /// Only meaningful for combat-flavored, not-yet-entered nodes — raising the risk of
    /// a node you can't avoid, or one that isn't dangerous to begin with, makes no sense.
    /// </summary>
    public void RaiseRisk()
    {
        if (!IsCombatFlavored(EventType))
        {
            throw new DomainException($"Only combat-flavored nodes can have their risk raised; '{EventType}' is not one.");
        }

        if (CombatRiskTier is null)
        {
            throw new DomainException("This combat node has no CombatRiskTier to raise.");
        }

        if (State != NodeState.Available)
        {
            throw new DomainException("Only an available MapNode's risk can be raised.");
        }

        if (CombatRiskTier == RiskTier.Fatal)
        {
            throw new DomainException("This node's risk is already at the maximum tier (Fatal).");
        }

        CombatRiskTier += 1;
    }

    /// <summary>
    /// Room-wide difficulty selector: sets this node's tier directly to the chosen value,
    /// in either direction — unlike <see cref="RaiseRisk"/>'s one-step-up-only "provoquer
    /// le destin" wager, this lets the player pick a difficulty once for the whole room
    /// before any of its combat nodes is triggered on contact.
    /// </summary>
    public void SetCombatRiskTier(RiskTier tier)
    {
        if (!IsCombatFlavored(EventType))
        {
            throw new DomainException($"Only combat-flavored nodes can have their risk tier set; '{EventType}' is not one.");
        }

        if (CombatRiskTier is null)
        {
            throw new DomainException("This combat node has no CombatRiskTier to set.");
        }

        if (State != NodeState.Available)
        {
            throw new DomainException("Only an available MapNode's risk tier can be set.");
        }

        CombatRiskTier = tier;
    }

    /// <summary>
    /// Every grid node starts <see cref="NodeState.Available"/> (free exploration has no
    /// row-unlock progression to replay), used when rolling back a room (e.g. mid-room exit).
    /// </summary>
    /// <summary>
    /// Found by searching the ground around it. Same guard-then-mutate shape as the other
    /// lifecycle methods: only a node that is actually hiding can be revealed, so a caller
    /// cannot quietly "reveal" an ordinary node and change nothing.
    /// </summary>
    public void Reveal()
    {
        if (HiddenState != HiddenState.Hint)
        {
            throw new DomainException("Only a hidden MapNode can be revealed.");
        }

        HiddenState = HiddenState.Revealed;
    }

    public void ResetToGridAvailable()
    {
        State = NodeState.Available;
        ChosenEventOptionId = null;
    }

    public void ChooseEventOption(string choiceId)
    {
        if (State != NodeState.Resolved)
        {
            throw new DomainException("Only a resolved MapNode can receive an event choice.");
        }

        if (string.IsNullOrWhiteSpace(choiceId))
        {
            throw new DomainException("Event choice id is required.");
        }

        if (HasChosenEventOption)
        {
            throw new DomainException("Current event choice has already been resolved.");
        }

        ChosenEventOptionId = choiceId.Trim();
    }

    public static MapNode Rehydrate(
        NodeId id,
        NodeEventType eventType,
        int row,
        int lane,
        int riskLevel,
        string rewardProfile,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isBoss,
        NodeState state,
        string? chosenEventOptionId,
        RiskTier? combatRiskTier = null,
        HiddenState hiddenState = HiddenState.None,
        DangerTell dangerTell = DangerTell.None,
        ContactBehavior contactBehavior = ContactBehavior.None,
        string? exitDestinationRoomKey = null,
        string? exitDestinationDisplayName = null)
    {
        var node = new MapNode(
            id, eventType, row, lane, riskLevel, combatRiskTier, rewardProfile, parentNodeIds,
            isBoss, state, hiddenState, dangerTell, contactBehavior,
            exitDestinationRoomKey, exitDestinationDisplayName);
        node.ChosenEventOptionId = chosenEventOptionId;
        return node;
    }
}
