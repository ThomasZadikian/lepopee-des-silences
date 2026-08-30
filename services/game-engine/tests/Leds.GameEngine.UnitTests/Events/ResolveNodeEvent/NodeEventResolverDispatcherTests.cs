using FluentAssertions;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ResolveNodeEvent;

public sealed class NodeEventResolverDispatcherTests
{
    [Fact]
    public void Resolve_ShouldDelegateToRegisteredResolver()
    {
        var run = TestGameEngineFactory.CreateRun();
        var node = run.CurrentRoom.AvailableNodes.First();

        var context = new NodeEventResolutionContext(run, run.CurrentRoom, node);

        var expected = NodeEventResolutionResult.Create(
            NodeEventResolutionKind.NarrativeFragmentRevealed,
            "Test outcome",
            "Test description");

        var resolver = new Mock<INodeEventResolver>();
        resolver.Setup(r => r.EventType).Returns(NodeEventType.Combat);
        resolver.Setup(r => r.Resolve(context)).Returns(expected);

        var dispatcher = new NodeEventResolverDispatcher(new[] { resolver.Object });

        var result = dispatcher.Resolve(context);

        result.Should().Be(expected);
        resolver.Verify(r => r.Resolve(context), Times.Once);
    }

    [Fact]
    public void Resolve_ShouldThrowDomainException_WhenNoResolverRegistered()
    {
        var run = TestGameEngineFactory.CreateRun();
        var rareNode = MapNode.Create(
            NodeEventType.Rare,
            40,
            "rare",
            1,
            0,
            run.CurrentRoom.AvailableNodes.Select(n => n.Id).ToArray(),
            false,
            NodeState.Planned);

        var context = new NodeEventResolutionContext(run, run.CurrentRoom, rareNode);

        var combatResolver = new Mock<INodeEventResolver>();
        combatResolver.Setup(r => r.EventType).Returns(NodeEventType.Combat);

        var dispatcher = new NodeEventResolverDispatcher(new[] { combatResolver.Object });

        var act = () => dispatcher.Resolve(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*no resolver registered*");
    }

    [Fact]
    public void Resolve_ShouldUseCorrectResolver_ForMultipleRegisteredResolvers()
    {
        var combatRun = TestGameEngineFactory.CreateRun(NodeEventType.Combat);
        var combatNode = combatRun.CurrentRoom.AvailableNodes.First();

        var itemRun = TestGameEngineFactory.CreateRun(NodeEventType.Item);
        var itemNode = itemRun.CurrentRoom.AvailableNodes.First();

        var combatResult = NodeEventResolutionResult.Create(
            NodeEventResolutionKind.CombatStarted,
            "Combat",
            "Combat started");

        var itemResult = NodeEventResolutionResult.Create(
            NodeEventResolutionKind.NarrativeFragmentRevealed,
            "Item",
            "Item found");

        var combatResolver = new Mock<INodeEventResolver>();
        combatResolver.Setup(r => r.EventType).Returns(NodeEventType.Combat);
        combatResolver.Setup(r => r.Resolve(It.IsAny<NodeEventResolutionContext>())).Returns(combatResult);

        var itemResolver = new Mock<INodeEventResolver>();
        itemResolver.Setup(r => r.EventType).Returns(NodeEventType.Item);
        itemResolver.Setup(r => r.Resolve(It.IsAny<NodeEventResolutionContext>())).Returns(itemResult);

        var dispatcher = new NodeEventResolverDispatcher(new[] { combatResolver.Object, itemResolver.Object });

        var combatContext = new NodeEventResolutionContext(combatRun, combatRun.CurrentRoom, combatNode);
        var itemContext = new NodeEventResolutionContext(itemRun, itemRun.CurrentRoom, itemNode);

        var combatResolution = dispatcher.Resolve(combatContext);
        var itemResolution = dispatcher.Resolve(itemContext);

        combatResolution.Should().Be(combatResult);
        itemResolution.Should().Be(itemResult);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenResolverCollectionIsEmpty()
    {
        var run = TestGameEngineFactory.CreateRun();
        var node = run.CurrentRoom.AvailableNodes.First();

        var context = new NodeEventResolutionContext(run, run.CurrentRoom, node);

        var dispatcher = new NodeEventResolverDispatcher(Array.Empty<INodeEventResolver>());

        var act = () => dispatcher.Resolve(context);

        act.Should().Throw<DomainException>()
            .WithMessage("*no resolver registered*");
    }
}
