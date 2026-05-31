using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Risk;

namespace Leds.GameEngine.UnitTests.Generation.Rooms.Risk;

public sealed class NodeRiskResolverTests
{
    private readonly NodeRiskResolver _sut = new();

    [Fact]
    public void Resolve_ShouldKeepRestRiskLow()
    {
        // Act
        var risk = ResolveSingle(NodeEventType.Rest);

        // Assert
        risk.Should().BeInRange(0, 10);
    }

    [Fact]
    public void Resolve_ShouldMakeEliteRiskGreaterThanCombatRisk()
    {
        // Act
        var combatRisk = ResolveSingle(NodeEventType.Combat);
        var eliteRisk = ResolveSingle(NodeEventType.Elite);

        // Assert
        eliteRisk.Should().BeGreaterThan(combatRisk);
    }

    [Fact]
    public void Resolve_ShouldMakeRoomBossRiskGreaterThanEliteRisk()
    {
        // Act
        var eliteRisk = ResolveSingle(NodeEventType.Elite);
        var bossRisk = ResolveSingle(NodeEventType.RoomBoss);

        // Assert
        bossRisk.Should().BeGreaterThan(eliteRisk);
    }

    [Fact]
    public void Resolve_ShouldKeepFinalBossRiskNearMaximum()
    {
        // Act
        var risk = ResolveSingle(NodeEventType.FinalBoss);

        // Assert
        risk.Should().BeInRange(95, 100);
    }

    [Fact]
    public void Resolve_ShouldIncreaseRisk_WhenCombiningDangerousEvents()
    {
        // Arrange
        var combatOnlyRisk = ResolveSingle(NodeEventType.Combat);

        var dangerousEvents = new[]
        {
            NodeEvent.Create(NodeEventType.Combat, order: 1),
            NodeEvent.Create(NodeEventType.Curse, order: 2)
        };

        // Act
        var combinedRisk = _sut.Resolve(dangerousEvents, new Random(0));

        // Assert
        combinedRisk.Should().BeGreaterThan(combatOnlyRisk);
    }

    private int ResolveSingle(NodeEventType eventType)
    {
        var events = new[]
        {
            NodeEvent.Create(eventType, order: 1)
        };

        return _sut.Resolve(events, new Random(0));
    }
}