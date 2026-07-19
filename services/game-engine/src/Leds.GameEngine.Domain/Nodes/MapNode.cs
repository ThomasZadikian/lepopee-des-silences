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
        NodeState state)
    {
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
    }

    public NodeId Id { get; }

    public NodeEventType EventType { get; }

    public int Row { get; }

    public int Lane { get; }

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
        RiskTier? combatRiskTier = null)
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
            initialState);
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

    public void ResetToInitial()
    {
        State = IsInitial ? NodeState.Available : NodeState.Planned;
        ChosenEventOptionId = null;
    }

    /// <summary>
    /// Grid-mode counterpart of <see cref="ResetToInitial"/> — every grid node starts
    /// <see cref="NodeState.Available"/> (free exploration has no row-unlock progression to
    /// replay), used when rolling back a Tactical-mode room (e.g. mid-room exit).
    /// </summary>
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
        RiskTier? combatRiskTier = null)
    {
        var node = new MapNode(id, eventType, row, lane, riskLevel, combatRiskTier, rewardProfile, parentNodeIds, isBoss, state);
        node.ChosenEventOptionId = chosenEventOptionId;
        return node;
    }
}