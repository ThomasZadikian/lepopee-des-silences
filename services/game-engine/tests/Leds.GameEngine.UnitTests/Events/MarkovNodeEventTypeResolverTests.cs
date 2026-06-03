using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Events;

public sealed class MarkovNodeEventTypeResolverTests
{
    private readonly MarkovNodeEventTypeResolver _resolver = new(
        new StaticNodeEventTypeMarkovMatrixProvider(),
        new MarkovTransitionResolver(new DeterministicMarkovSampler()));

    [Fact]
    public void ResolveNextEventType_ShouldReturnSampledType_WhenAllowed()
    {
        var context = CreateContext(
            roomType: RoomType.Forest,
            allowedEventTypes: new[]
            {
                NodeEventType.Combat,
                NodeEventType.Elite,
                NodeEventType.Item
            });

        var result = _resolver.ResolveNextEventType(context, sample: 0.40m);

        result.Should().Be(NodeEventType.Elite);
    }

    [Fact]
    public void ResolveNextEventType_ShouldFallbackToHighestProbabilityAllowedType_WhenSampledTypeIsNotAllowed()
    {
        var context = CreateContext(
            roomType: RoomType.Forest,
            allowedEventTypes: new[]
            {
                NodeEventType.Item,
                NodeEventType.Npc
            });

        var result = _resolver.ResolveNextEventType(context, sample: 0.01m);

        result.Should().Be(NodeEventType.Item);
    }

    [Fact]
    public void ResolveNextEventType_ShouldUseRoomTypeAnchor_WhenPreviousEventTypeIsNull()
    {
        var context = CreateContext(
            roomType: RoomType.Silence,
            previousEventType: null,
            allowedEventTypes: new[]
            {
                NodeEventType.Law,
                NodeEventType.Combat
            });

        var result = _resolver.ResolveNextEventType(context, sample: 0.70m);

        result.Should().Be(NodeEventType.Law);
    }

    [Fact]
    public void ResolveNextEventType_ShouldUsePreviousEventType_WhenProvided()
    {
        var context = CreateContext(
            roomType: RoomType.Silence,
            previousEventType: NodeEventType.Item,
            allowedEventTypes: new[]
            {
                NodeEventType.Item,
                NodeEventType.Combat
            });

        var result = _resolver.ResolveNextEventType(context, sample: 0.30m);

        result.Should().Be(NodeEventType.Item);
    }

    [Fact]
    public void ResolveNextEventType_ShouldBeDeterministic_ForSameInputs()
    {
        var context = CreateContext(
            roomType: RoomType.Rupture,
            previousEventType: NodeEventType.Curse,
            allowedEventTypes: new[]
            {
                NodeEventType.Combat,
                NodeEventType.Elite,
                NodeEventType.Curse,
                NodeEventType.Rare
            });

        var first = _resolver.ResolveNextEventType(context);
        var second = _resolver.ResolveNextEventType(context);

        second.Should().Be(first);
    }

    [Fact]
    public void ResolveNextEventType_ShouldIgnoreMemory_WhenMemoryIsProvidedAsAllowed()
    {
        var context = CreateContext(
            roomType: RoomType.Memory,
            allowedEventTypes: new[]
            {
                NodeEventType.Memory,
                NodeEventType.Combat
            });

        var result = _resolver.ResolveNextEventType(context, sample: 0.01m);

        result.Should().Be(NodeEventType.Combat);
    }

    [Fact]
    public void ResolveNextEventType_ShouldThrow_WhenOnlyNonPlannableTypesAreAllowed()
    {
        var context = CreateContext(
            roomType: RoomType.Memory,
            allowedEventTypes: new[]
            {
                NodeEventType.Memory,
                NodeEventType.RoomBoss
            });

        var act = () => _resolver.ResolveNextEventType(context, sample: 0.01m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No plannable node event type is allowed by the current constraints.");
    }

    [Fact]
    public void ResolveNextEventType_ShouldThrow_WhenMatrixVersionIsUnsupported()
    {
        var context = CreateContext(
            matrixVersion: "markov-node-event-type-unknown",
            roomType: RoomType.Forest,
            allowedEventTypes: new[]
            {
                NodeEventType.Combat
            });

        var act = () => _resolver.ResolveNextEventType(context, sample: 0.01m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported node event type Markov matrix version 'markov-node-event-type-unknown'.");
    }

    private static NodeEventTypeSelectionContext CreateContext(
        string seed = "seed-test-001",
        string matrixVersion = StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
        RoomType roomType = RoomType.Forest,
        int roomDepth = 1,
        int nodeDepth = 2,
        int eventIndex = 0,
        NodeEventType? previousEventType = null,
        IReadOnlyCollection<NodeEventType>? allowedEventTypes = null)
    {
        return new NodeEventTypeSelectionContext(
            Seed: seed,
            MatrixVersion: matrixVersion,
            RoomType: roomType,
            RoomDepth: roomDepth,
            NodeDepth: nodeDepth,
            EventIndex: eventIndex,
            PreviousEventType: previousEventType,
            AllowedEventTypes: allowedEventTypes ?? new[] { NodeEventType.Combat });
    }
}