using FluentAssertions;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Events;

public sealed class NodeEventGeneratorTests
{
    private const string Seed = "seed-node-event-generator-tests";

    [Fact]
    public void Generate_ShouldReturnBetweenOneAndFourEvents()
    {
        // Arrange
        var sut = CreateSut(new NodeEventCandidateResolver());
        var state = new RoomEventGenerationState();

        // Act
        var result = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Threshold,
            roomDepth: 0,
            nodeDepth: 0,
            totalNodeCount: 8,
            state);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Should().HaveCountLessThanOrEqualTo(4);
    }

    [Fact]
    public void Generate_ShouldNotGenerateMemoryOrBossEvents_ForStandardNode()
    {
        // Arrange
        var sut = CreateSut(new NodeEventCandidateResolver());
        var state = new RoomEventGenerationState();

        // Act
        var result = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Threshold,
            roomDepth: 0,
            nodeDepth: 0,
            totalNodeCount: 8,
            state);

        // Assert
        result.Select(nodeEvent => nodeEvent.EventType)
            .Should()
            .NotContain(NodeEventType.Memory);

        result.Select(nodeEvent => nodeEvent.EventType)
            .Should()
            .NotContain(NodeEventType.RoomBoss);

        result.Select(nodeEvent => nodeEvent.EventType)
            .Should()
            .NotContain(NodeEventType.FinalBoss);
    }

    [Fact]
    public void Generate_ShouldReturnOnlyRest_WhenRestIsTheFirstGeneratedEvent()
    {
        // Arrange
        var sut = CreateSut(new FixedCandidateResolver(NodeEventType.Rest));
        var state = new RoomEventGenerationState();

        // Act
        var result = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Threshold,
            roomDepth: 0,
            nodeDepth: 0,
            totalNodeCount: 8,
            state);

        // Assert
        result.Should().ContainSingle();
        result.Single().EventType.Should().Be(NodeEventType.Rest);
        state.RestCount.Should().Be(1);
    }

    [Fact]
    public void Generate_ShouldRegisterGeneratedEventsInRoomState()
    {
        // Arrange
        var sut = CreateSut(new FixedCandidateResolver(NodeEventType.Rare));
        var state = new RoomEventGenerationState();

        // Act
        var result = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Threshold,
            roomDepth: 0,
            nodeDepth: 0,
            totalNodeCount: 8,
            state);

        // Assert
        result.Should().NotBeEmpty();
        state.RareCount.Should().Be(result.Count);
    }

    [Fact]
    public void Generate_ShouldRespectCandidateResolverOutput()
    {
        // Arrange
        var sut = CreateSut(new FixedCandidateResolver(NodeEventType.Combat));
        var state = new RoomEventGenerationState();

        // Act
        var result = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Threshold,
            roomDepth: 0,
            nodeDepth: 0,
            totalNodeCount: 8,
            state);

        // Assert
        result.Should().OnlyContain(nodeEvent =>
            nodeEvent.EventType == NodeEventType.Combat);
    }

    [Fact]
    public void Generate_ShouldBeDeterministic_ForSameInputs()
    {
        // Arrange
        var sut = CreateSut(new NodeEventCandidateResolver());

        var firstState = new RoomEventGenerationState();
        var secondState = new RoomEventGenerationState();

        // Act
        var first = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Forest,
            roomDepth: 1,
            nodeDepth: 2,
            totalNodeCount: 8,
            firstState);

        var second = sut.Generate(
            new Random(0),
            Seed,
            StaticNodeEventTypeMarkovMatrixProvider.SupportedVersion,
            RoomType.Forest,
            roomDepth: 1,
            nodeDepth: 2,
            totalNodeCount: 8,
            secondState);

        // Assert
        second.Select(nodeEvent => nodeEvent.EventType)
            .Should()
            .Equal(first.Select(nodeEvent => nodeEvent.EventType));
    }

    private static NodeEventGenerator CreateSut(
        INodeEventCandidateResolver candidateResolver)
    {
        return new NodeEventGenerator(
            candidateResolver,
            new MarkovNodeEventTypeResolver(
                new StaticNodeEventTypeMarkovMatrixProvider(),
                new MarkovTransitionResolver(new DeterministicMarkovSampler())));
    }

    private sealed class FixedCandidateResolver : INodeEventCandidateResolver
    {
        private readonly IReadOnlyList<NodeEventType> _candidates;

        public FixedCandidateResolver(params NodeEventType[] candidates)
        {
            _candidates = candidates;
        }

        public IReadOnlyList<NodeEventType> ResolveCandidates(
            RoomType roomType,
            int totalNodeCount,
            IRoomEventGenerationState state,
            IReadOnlyCollection<NodeEvent> currentNodeEvents)
        {
            return _candidates;
        }
    }
}