using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;

namespace Leds.GameEngine.Domain.Nodes;

public sealed class Node
{
    private readonly List<NodeEvent> _events;
    private readonly List<NodeId> _parentNodeIds;

    private Node(
        NodeId id,
        IReadOnlyCollection<NodeEvent> events,
        int riskLevel,
        string rewardProfile,
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isRoomBossNode,
        NodeState state)
    {
        Id = id;
        _events = events.ToList();
        RiskLevel = riskLevel;
        RewardProfile = rewardProfile;
        NodeDepth = nodeDepth;
        _parentNodeIds = parentNodeIds.ToList();
        IsRoomBossNode = isRoomBossNode;
        State = state;
    }

    public NodeId Id { get; }

    public IReadOnlyCollection<NodeEvent> Events => _events.AsReadOnly();

    public IReadOnlyCollection<NodeEventType> EventTypes => _events
        .OrderBy(nodeEvent => nodeEvent.Order)
        .Select(nodeEvent => nodeEvent.EventType)
        .ToArray();

    public int EventCount => _events.Count;

    public NodeEvent PrimaryEvent => _events
        .OrderBy(nodeEvent => nodeEvent.Order)
        .First();

    public NodeEventType EventType => PrimaryEvent.EventType;

    public NodeEvent? ResolvedEvent => _events
        .SingleOrDefault(nodeEvent => nodeEvent.IsResolved);

    public bool HasResolvedEvent => ResolvedEvent is not null;

    public IReadOnlyCollection<NodeEvent> ClosedEvents => _events
        .Where(nodeEvent => nodeEvent.IsClosed)
        .OrderBy(nodeEvent => nodeEvent.Order)
        .ToArray();

    public int RiskLevel { get; }

    public string RewardProfile { get; }

    public int NodeDepth { get; }

    public IReadOnlyCollection<NodeId> ParentNodeIds => _parentNodeIds.AsReadOnly();

    public NodeId? ParentNodeId => _parentNodeIds.Count == 0
        ? null
        : _parentNodeIds.First();

    public bool IsRoomBossNode { get; }

    public NodeState State { get; private set; }

    public string? ChosenEventOptionId { get; private set; }

    public bool HasChosenEventOption => !string.IsNullOrWhiteSpace(ChosenEventOptionId);

    public bool IsAvailable => State == NodeState.Available;

    public bool IsPlanned => State == NodeState.Planned;

    public static Node Create(
        NodeEventType eventType,
        int riskLevel,
        string rewardProfile,
        int nodeDepth = 0,
        NodeId? parentNodeId = null,
        bool isRoomBossNode = false,
        NodeState initialState = NodeState.Available)
    {
        var parentNodeIds = parentNodeId is null
            ? Array.Empty<NodeId>()
            : new[] { parentNodeId.Value };

        return Create(
            new[] { NodeEvent.Create(eventType, 1) },
            riskLevel,
            rewardProfile,
            nodeDepth,
            parentNodeIds,
            isRoomBossNode,
            initialState);
    }

    public static Node Create(
        IReadOnlyCollection<NodeEvent> events,
        int riskLevel,
        string rewardProfile,
        int nodeDepth,
        NodeId? parentNodeId,
        bool isRoomBossNode,
        NodeState initialState)
    {
        var parentNodeIds = parentNodeId is null
            ? Array.Empty<NodeId>()
            : new[] { parentNodeId.Value };

        return Create(
            events,
            riskLevel,
            rewardProfile,
            nodeDepth,
            parentNodeIds,
            isRoomBossNode,
            initialState);
    }

    public static Node Create(
        IReadOnlyCollection<NodeEvent> events,
        int riskLevel,
        string rewardProfile,
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds,
        bool isRoomBossNode,
        NodeState initialState)
    {
        if (riskLevel is < 0 or > 100)
        {
            throw new DomainException("Node risk level must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(rewardProfile))
        {
            throw new DomainException("Node reward profile is required.");
        }

        if (nodeDepth < 0)
        {
            throw new DomainException("Node depth must be greater than or equal to 0.");
        }

        if (initialState is not NodeState.Planned and not NodeState.Available)
        {
            throw new DomainException("A newly created node must be Planned or Available.");
        }

        var eventList = events?.OrderBy(nodeEvent => nodeEvent.Order).ToList()
            ?? throw new DomainException("Node events are required.");

        if (eventList.Count is < 1 or > 4)
        {
            throw new DomainException("A node must contain between 1 and 4 events.");
        }

        if (eventList.Select(nodeEvent => nodeEvent.Order).Distinct().Count() != eventList.Count)
        {
            throw new DomainException("Node event orders must be unique.");
        }

        if (eventList.Any(nodeEvent => nodeEvent.Status != NodeEventStatus.Planned))
        {
            throw new DomainException("Newly created node events must be planned.");
        }

        var parentList = parentNodeIds?.Distinct().ToList()
            ?? throw new DomainException("Parent node ids are required.");

        if (nodeDepth == 0 && parentList.Count != 0)
        {
            throw new DomainException("Initial layer nodes cannot have parents.");
        }

        if (nodeDepth > 0 && parentList.Count == 0)
        {
            throw new DomainException("Non-initial nodes must have at least one parent.");
        }

        var hasRoomBossEvent = eventList.Any(nodeEvent =>
            nodeEvent.EventType == NodeEventType.RoomBoss);

        var hasFinalBossEvent = eventList.Any(nodeEvent =>
            nodeEvent.EventType == NodeEventType.FinalBoss);

        if (isRoomBossNode && !hasRoomBossEvent)
        {
            throw new DomainException("A room boss node must contain a RoomBoss event.");
        }

        if (!isRoomBossNode && hasRoomBossEvent)
        {
            throw new DomainException("A RoomBoss event can only appear on a room boss node.");
        }

        if (hasFinalBossEvent && !isRoomBossNode)
        {
            throw new DomainException("A FinalBoss event can only appear on a boss node.");
        }

        return new Node(
            NodeId.New(),
            eventList,
            riskLevel,
            rewardProfile.Trim(),
            nodeDepth,
            parentList,
            isRoomBossNode,
            initialState);
    }

    public void Unlock()
    {
        if (State != NodeState.Planned)
        {
            throw new DomainException("Only a planned node can be unlocked.");
        }

        State = NodeState.Available;
    }

    public void Select()
    {
        if (State != NodeState.Available)
        {
            throw new DomainException("Only an available node can be selected.");
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

    public NodeEvent ResolvePrimaryEvent()
    {
        return ResolveEvent(PrimaryEvent.Order);
    }

    public NodeEvent ResolveEvent(int eventOrder)
    {
        if (State != NodeState.Selected)
        {
            throw new DomainException("Only a selected node can resolve an event.");
        }

        if (HasResolvedEvent)
        {
            throw new DomainException("Node event has already been resolved.");
        }

        var selectedEvent = _events.SingleOrDefault(nodeEvent =>
            nodeEvent.Order == eventOrder);

        if (selectedEvent is null)
        {
            throw new DomainException($"Node event with order '{eventOrder}' was not found.");
        }

        if (!selectedEvent.IsPlanned)
        {
            throw new DomainException("Only a planned node event can be resolved.");
        }

        selectedEvent.Resolve();

        foreach (var alternativeEvent in _events.Where(nodeEvent =>
                     nodeEvent.Order != selectedEvent.Order))
        {
            alternativeEvent.Close();
        }

        State = NodeState.Resolved;

        return selectedEvent;
    }

    public void Resolve()
    {
        ResolvePrimaryEvent();
    }

    public void ChooseEventOption(string choiceId)
    {
        if (State != NodeState.Resolved)
        {
            throw new DomainException("Only a resolved node can receive an event choice.");
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