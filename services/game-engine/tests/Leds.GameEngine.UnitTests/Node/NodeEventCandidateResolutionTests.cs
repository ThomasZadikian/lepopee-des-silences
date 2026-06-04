using FluentAssertions;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class NodeEventCandidateResolutionTests
{
    [Fact]
    public void Resolve_ShouldResolvePrimaryEvent_AndCloseAlternativeEvents()
    {
        var node = Node.Create(
            new[]
            {
                NodeEvent.Create(NodeEventType.Combat, order: 1),
                NodeEvent.Create(NodeEventType.Item, order: 2),
                NodeEvent.Create(NodeEventType.Rare, order: 3)
            },
            riskLevel: 25,
            rewardProfile: "standard",
            nodeDepth: 0,
            parentNodeIds: Array.Empty<NodeId>(),
            isRoomBossNode: false,
            initialState: NodeState.Available);

        node.Select();

        node.Resolve();

        node.State.Should().Be(NodeState.Resolved);
        node.ResolvedEvent.Should().NotBeNull();
        node.ResolvedEvent!.EventType.Should().Be(NodeEventType.Combat);

        node.Events.Single(nodeEvent => nodeEvent.Order == 1)
            .Status.Should().Be(NodeEventStatus.Resolved);

        node.Events.Single(nodeEvent => nodeEvent.Order == 2)
            .Status.Should().Be(NodeEventStatus.Closed);

        node.Events.Single(nodeEvent => nodeEvent.Order == 3)
            .Status.Should().Be(NodeEventStatus.Closed);
    }
}