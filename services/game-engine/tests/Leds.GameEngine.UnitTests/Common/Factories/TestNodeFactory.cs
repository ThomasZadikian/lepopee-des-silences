using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestNodeFactory
{
    public static Node CreateNode(
        NodeEventType eventType,
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned,
        int riskLevel = 20,
        string rewardProfile = "common")
    {
        return Node.Create(
            new[]
            {
                NodeEvent.Create(eventType, order: 1)
            },
            riskLevel,
            rewardProfile,
            nodeDepth,
            parentNodeIds ?? Array.Empty<NodeId>(),
            isRoomBossNode: eventType == NodeEventType.RoomBoss,
            initialState);
    }

    public static Node CreateCombatNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned)
    {
        return CreateNode(
            NodeEventType.Combat,
            nodeDepth,
            parentNodeIds,
            initialState,
            riskLevel: 30,
            rewardProfile: "combat-common");
    }

    public static Node CreateMemoryNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned)
    {
        return CreateNode(
            NodeEventType.Memory,
            nodeDepth,
            parentNodeIds,
            initialState,
            riskLevel: 10,
            rewardProfile: "narrative");
    }

    public static Node CreateRestNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned)
    {
        return CreateNode(
            NodeEventType.Rest,
            nodeDepth,
            parentNodeIds,
            initialState,
            riskLevel: 5,
            rewardProfile: "none");
    }

    public static Node CreateItemNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned)
    {
        return CreateNode(
            NodeEventType.Item,
            nodeDepth,
            parentNodeIds,
            initialState,
            riskLevel: 15,
            rewardProfile: "common");
    }

    public static Node CreateRoomBossNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId> parentNodeIds,
        NodeState initialState = NodeState.Planned)
    {
        return Node.Create(
            new[]
            {
                NodeEvent.Create(NodeEventType.RoomBoss, order: 1)
            },
            riskLevel: 85,
            rewardProfile: "room-boss",
            nodeDepth,
            parentNodeIds,
            isRoomBossNode: true,
            initialState);
    }

    public static Node CreateMultiEventNode(
        int nodeDepth,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        NodeState initialState = NodeState.Planned)
    {
        return Node.Create(
            new[]
            {
                NodeEvent.Create(NodeEventType.Memory, order: 1),
                NodeEvent.Create(NodeEventType.Combat, order: 2)
            },
            riskLevel: 35,
            rewardProfile: "mixed",
            nodeDepth,
            parentNodeIds ?? Array.Empty<NodeId>(),
            isRoomBossNode: false,
            initialState);
    }
}