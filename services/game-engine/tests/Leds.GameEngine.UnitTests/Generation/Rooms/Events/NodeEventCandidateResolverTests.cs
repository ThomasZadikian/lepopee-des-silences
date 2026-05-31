using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Events;

public sealed class NodeEventCandidateResolverTests
{
    private readonly NodeEventCandidateResolver _sut = new();

    [Fact]
    public void ResolveCandidates_ShouldNeverReturnMemoryOrBossEvents_ForStandardNode()
    {
        // Act
        var result = Resolve();

        // Assert
        result.Should().NotContain(NodeEventType.Memory);
        result.Should().NotContain(NodeEventType.RoomBoss);
        result.Should().NotContain(NodeEventType.FinalBoss);
    }

    [Fact]
    public void ResolveCandidates_ShouldRemoveRest_WhenCurrentNodeAlreadyHasAnEvent()
    {
        // Arrange
        var currentNodeEvents = new[]
        {
            NodeEvent.Create(NodeEventType.Combat, order: 1)
        };

        // Act
        var result = Resolve(currentNodeEvents: currentNodeEvents);

        // Assert
        result.Should().NotContain(NodeEventType.Rest);
    }

    [Fact]
    public void ResolveCandidates_ShouldRemoveItem_WhenCurrentNodeAlreadyContainsItem()
    {
        // Arrange
        var currentNodeEvents = new[]
        {
            NodeEvent.Create(NodeEventType.Item, order: 1)
        };

        // Act
        var result = Resolve(currentNodeEvents: currentNodeEvents);

        // Assert
        result.Should().NotContain(NodeEventType.Item);
    }

    [Theory]
    [InlineData(NodeEventType.Elite)]
    [InlineData(NodeEventType.Npc)]
    [InlineData(NodeEventType.Rest)]
    [InlineData(NodeEventType.Merchant)]
    [InlineData(NodeEventType.Law)]
    [InlineData(NodeEventType.Curse)]
    public void ResolveCandidates_ShouldRemoveSingleOccurrenceEvents_WhenAlreadyPresentInRoom(
        NodeEventType eventType)
    {
        // Arrange
        var state = StateWith(eventType);

        // Act
        var result = Resolve(state: state);

        // Assert
        result.Should().NotContain(eventType);
    }

    [Fact]
    public void ResolveCandidates_ShouldRemoveRare_WhenRareLimitIsReached()
    {
        // Arrange
        var state = StateWith(
            NodeEventType.Rare,
            NodeEventType.Rare,
            NodeEventType.Rare);

        // Act
        var result = Resolve(state: state);

        // Assert
        result.Should().NotContain(NodeEventType.Rare);
    }

    [Fact]
    public void ResolveCandidates_ShouldRemoveItem_WhenRoomItemLimitIsReached()
    {
        // Arrange
        const int totalNodeCount = 8;

        var state = StateWith(
            NodeEventType.Item,
            NodeEventType.Item,
            NodeEventType.Item,
            NodeEventType.Item);

        // Act
        var result = Resolve(
            state: state,
            totalNodeCount: totalNodeCount);

        // Assert
        result.Should().NotContain(NodeEventType.Item);
    }

    [Fact]
    public void ResolveCandidates_ShouldRestrictCandidates_ForFinalRoom()
    {
        // Act
        var result = Resolve(roomType: RoomType.Final);

        // Assert
        result.Should().OnlyContain(eventType =>
            eventType == NodeEventType.Combat
                || eventType == NodeEventType.Elite
                || eventType == NodeEventType.Curse
                || eventType == NodeEventType.Rare);
    }

    [Fact]
    public void ResolveCandidates_ShouldAlwaysKeepCombatAsFallbackCandidate()
    {
        // Arrange
        var state = StateWith(
            NodeEventType.Elite,
            NodeEventType.Npc,
            NodeEventType.Rest,
            NodeEventType.Merchant,
            NodeEventType.Law,
            NodeEventType.Curse,
            NodeEventType.Rare,
            NodeEventType.Rare,
            NodeEventType.Rare,
            NodeEventType.Item,
            NodeEventType.Item,
            NodeEventType.Item,
            NodeEventType.Item);

        // Act
        var result = Resolve(
            state: state,
            totalNodeCount: 8);

        // Assert
        result.Should().Contain(NodeEventType.Combat);
    }

    private IReadOnlyList<NodeEventType> Resolve(
        RoomType roomType = RoomType.Threshold,
        int totalNodeCount = 8,
        IRoomEventGenerationState? state = null,
        IReadOnlyCollection<NodeEvent>? currentNodeEvents = null)
    {
        return _sut.ResolveCandidates(
            roomType,
            totalNodeCount,
            state ?? new RoomEventGenerationState(),
            currentNodeEvents ?? Array.Empty<NodeEvent>());
    }

    private static IRoomEventGenerationState StateWith(params NodeEventType[] eventTypes)
    {
        var state = new RoomEventGenerationState();

        foreach (var eventType in eventTypes)
        {
            state.Register(eventType);
        }

        return state;
    }
}