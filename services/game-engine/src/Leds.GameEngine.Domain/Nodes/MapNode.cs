using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;

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
        RewardProfile = rewardProfile;
        _parentNodeIds = parentNodeIds.ToList();
        IsBoss = isBoss;
        State = state;
    }

    public NodeId Id { get; }

    public NodeEventType EventType { get; }

    public int Row { get; }

    public int Lane { get; }

    public int RiskLevel { get; }

    public string RewardProfile { get; }

    public IReadOnlyCollection<NodeId> ParentNodeIds => _parentNodeIds.AsReadOnly();

    public bool IsBoss { get; }

    public NodeState State { get; private set; }

    public bool IsInitial => Row == 0 && _parentNodeIds.Count == 0 && !IsBoss;

    public bool IsAvailable => State == NodeState.Available;

    public bool IsPlanned => State == NodeState.Planned;

    public string? ChosenEventOptionId { get; private set; }

    public bool HasChosenEventOption => !string.IsNullOrWhiteSpace(ChosenEventOptionId);

    public static MapNode Create(
        NodeEventType eventType,
        int riskLevel,
        string rewardProfile,
        int row,
        int lane,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isBoss = false,
        NodeState initialState = NodeState.Available)
    {
        if (riskLevel is < 0 or > 100)
        {
            throw new DomainException("MapNode risk level must be between 0 and 100.");
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
}
