using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Rewards;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Rewards;

public sealed class NodeRewardProfileResolverTests
{
    private readonly NodeRewardProfileResolver _sut = new();

    [Theory]
    [InlineData(NodeEventType.RoomBoss, "room-boss")]
    [InlineData(NodeEventType.Rare, "rare")]
    [InlineData(NodeEventType.Merchant, "trade")]
    [InlineData(NodeEventType.Rest, "healing-only")]
    [InlineData(NodeEventType.Combat, "combat-common")]
    [InlineData(NodeEventType.Elite, "combat-common")]
    [InlineData(NodeEventType.Law, "law-modifier")]
    [InlineData(NodeEventType.Curse, "high-risk")]
    [InlineData(NodeEventType.Npc, "narrative-choice")]
    public void Resolve_ShouldReturnExpectedProfile_ForDominantEvent(
        NodeEventType eventType,
        string expectedRewardProfile)
    {
        // Arrange
        var events = new[]
        {
            NodeEvent.Create(eventType, order: 1)
        };

        // Act
        var result = _sut.Resolve(events);

        // Assert
        result.Should().Be(expectedRewardProfile);
    }

    [Fact]
    public void Resolve_ShouldPrioritizeRoomBoss_WhenMultipleEventsArePresent()
    {
        // Arrange
        var events = new[]
        {
            NodeEvent.Create(NodeEventType.Combat, order: 1),
            NodeEvent.Create(NodeEventType.Rare, order: 2),
            NodeEvent.Create(NodeEventType.RoomBoss, order: 3)
        };

        // Act
        var result = _sut.Resolve(events);

        // Assert
        result.Should().Be("room-boss");
    }

    [Fact]
    public void Resolve_ShouldNotReturnHealingOnly_WhenRestIsCombinedWithAnotherEvent()
    {
        // Arrange
        var events = new[]
        {
            NodeEvent.Create(NodeEventType.Rest, order: 1),
            NodeEvent.Create(NodeEventType.Item, order: 2)
        };

        // Act
        var result = _sut.Resolve(events);

        // Assert
        result.Should().Be("common");
    }

    [Fact]
    public void Resolve_ShouldReturnCommon_WhenNoKnownRewardRuleMatches()
    {
        // Arrange
        var events = new[]
        {
            NodeEvent.Create(NodeEventType.Item, order: 1)
        };

        // Act
        var result = _sut.Resolve(events);

        // Assert
        result.Should().Be("common");
    }
}