using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.NodeEvents;

public sealed class NodeEventTests
{
    [Fact]
    public void Create_ShouldCreatePlannedNodeEvent()
    {
        var nodeEvent = NodeEvent.Create(NodeEventType.Combat, order: 1);

        nodeEvent.EventType.Should().Be(NodeEventType.Combat);
        nodeEvent.Order.Should().Be(1);
        nodeEvent.Status.Should().Be(NodeEventStatus.Planned);
    }

    [Fact]
    public void Resolve_ShouldMarkNodeEventAsResolved()
    {
        var nodeEvent = NodeEvent.Create(NodeEventType.Combat, order: 1);

        nodeEvent.Resolve();

        nodeEvent.Status.Should().Be(NodeEventStatus.Resolved);
    }

    [Fact]
    public void Close_ShouldMarkNodeEventAsClosed()
    {
        var nodeEvent = NodeEvent.Create(NodeEventType.Item, order: 2);

        nodeEvent.Close();

        nodeEvent.Status.Should().Be(NodeEventStatus.Closed);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenNodeEventIsClosed()
    {
        var nodeEvent = NodeEvent.Create(NodeEventType.Item, order: 2);
        nodeEvent.Close();

        var act = () => nodeEvent.Resolve();

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Closed node event cannot be resolved.");
    }
}