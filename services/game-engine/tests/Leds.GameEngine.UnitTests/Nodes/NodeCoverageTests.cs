using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class NodeCoverageTests
{
    [Fact]
    public void Create_ShouldValidateEnvelopeEventsParentsAndBossRules()
    {
        AssertInvalid(() => Node.Create(NodeEventType.Item, -1, "reward"), "risk level");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 101, "reward"), "risk level");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 10, " "), "reward profile");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 10, "reward", nodeDepth: -1), "depth");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 10, "reward", initialState: NodeState.Locked), "Planned or Available");

        AssertInvalid(() => Node.Create(null!, 10, "reward", 0, [], false, NodeState.Available), "events are required");
        AssertInvalid(() => Node.Create([], 10, "reward", 0, [], false, NodeState.Available), "between 1 and 4 events");
        AssertInvalid(() => Node.Create(
            [NodeEvent.Create(NodeEventType.Item, 1), NodeEvent.Create(NodeEventType.Rest, 1)],
            10, "reward", 0, [], false, NodeState.Available), "orders must be unique");

        var resolved = NodeEvent.Create(NodeEventType.Item, 1);
        resolved.Resolve();
        AssertInvalid(() => Node.Create([resolved], 10, "reward", 0, [], false, NodeState.Available), "must be planned");

        AssertInvalid(() => Node.Create([NodeEvent.Create(NodeEventType.Item, 1)], 10, "reward", 0, null!, false, NodeState.Available), "Parent node ids are required");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 10, "reward", 0, NodeId.New()), "Initial layer nodes cannot have parents");
        AssertInvalid(() => Node.Create([NodeEvent.Create(NodeEventType.Item, 1)], 10, "reward", 1, [], false, NodeState.Available), "must have at least one parent");
        AssertInvalid(() => Node.Create(NodeEventType.Item, 10, "reward", isRoomBossNode: true), "must contain a RoomBoss event");
        AssertInvalid(() => Node.Create(NodeEventType.RoomBoss, 10, "reward"), "only appear on a room boss node");
        AssertInvalid(() => Node.Create(NodeEventType.FinalBoss, 10, "reward"), "only appear on a boss node");

        var parent = NodeId.New();
        var node = Node.Create(
            [NodeEvent.Create(NodeEventType.Item, 2), NodeEvent.Create(NodeEventType.Rest, 1)],
            10, " reward ", 1, [parent, parent], false, NodeState.Planned);
        node.RewardProfile.Should().Be("reward");
        node.ParentNodeIds.Should().ContainSingle().Which.Should().Be(parent);
        node.ParentNodeId.Should().Be(parent);
        node.EventTypes.Should().Equal(NodeEventType.Rest, NodeEventType.Item);
        node.PrimaryEvent.Order.Should().Be(1);
        node.IsPlanned.Should().BeTrue();
    }

    [Fact]
    public void Lifecycle_ShouldCoverUnlockSelectLockAndUnreachableTransitions()
    {
        var planned = Node.Create(NodeEventType.Item, 10, "reward", initialState: NodeState.Planned);
        planned.Unlock();
        planned.IsAvailable.Should().BeTrue();
        AssertInvalid(planned.Unlock, "planned node");

        planned.Select();
        planned.State.Should().Be(NodeState.Selected);
        AssertInvalid(planned.Select, "available node");
        planned.Lock();
        planned.State.Should().Be(NodeState.Selected);
        planned.MarkUnreachable();
        planned.State.Should().Be(NodeState.Selected);

        var available = Node.Create(NodeEventType.Item, 10, "reward");
        available.Lock();
        available.State.Should().Be(NodeState.Locked);

        var plannedToLock = Node.Create(NodeEventType.Item, 10, "reward", initialState: NodeState.Planned);
        plannedToLock.Lock();
        plannedToLock.State.Should().Be(NodeState.Planned);
        plannedToLock.MarkUnreachable();
        plannedToLock.State.Should().Be(NodeState.Unreachable);

        var availableToUnreachable = Node.Create(NodeEventType.Item, 10, "reward");
        availableToUnreachable.MarkUnreachable();
        availableToUnreachable.State.Should().Be(NodeState.Unreachable);
    }

    [Fact]
    public void ResolveEvent_ShouldResolveChosenOrderCloseAlternativesAndRejectInvalidStates()
    {
        var first = NodeEvent.Create(NodeEventType.Item, 1);
        var second = NodeEvent.Create(NodeEventType.Rest, 2);
        var node = Node.Create([first, second], 10, "reward", 0, [], false, NodeState.Available);

        AssertInvalid(() => node.ResolveEvent(1), "selected node");
        node.Select();
        AssertInvalid(() => node.ResolveEvent(99), "was not found");

        var resolved = node.ResolveEvent(2);
        resolved.Should().BeSameAs(second);
        resolved.IsResolved.Should().BeTrue();
        first.IsClosed.Should().BeTrue();
        node.State.Should().Be(NodeState.Resolved);
        node.ResolvedEvent.Should().BeSameAs(second);
        node.HasResolvedEvent.Should().BeTrue();
        node.ClosedEvents.Should().ContainSingle().Which.Should().BeSameAs(first);
        AssertInvalid(() => node.ResolveEvent(1), "selected node");

        node.Lock();
        node.State.Should().Be(NodeState.Resolved);
        node.MarkUnreachable();
        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void ChooseEventOption_ShouldValidateStateIdentityAndSingleChoice()
    {
        var node = Node.Create(NodeEventType.Item, 10, "reward");
        AssertInvalid(() => node.ChooseEventOption("choice"), "resolved node");

        node.Select();
        node.Resolve();
        AssertInvalid(() => node.ChooseEventOption(" "), "choice id");
        node.ChooseEventOption(" choice.one ");
        node.ChosenEventOptionId.Should().Be("choice.one");
        node.HasChosenEventOption.Should().BeTrue();
        AssertInvalid(() => node.ChooseEventOption("choice.two"), "already been resolved");
    }

    [Fact]
    public void ParentNodeId_ShouldBeNullForInitialNode()
    {
        var node = Node.Create(NodeEventType.Item, 10, "reward");
        node.ParentNodeId.Should().BeNull();
        node.HasResolvedEvent.Should().BeFalse();
        node.ClosedEvents.Should().BeEmpty();
    }

    private static void AssertInvalid(Action action, string message) =>
        FluentActions.Invoking(action).Should().Throw<DomainException>().WithMessage($"*{message}*");
}
