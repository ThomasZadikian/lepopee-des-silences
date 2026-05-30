using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Infrastructure.Generation;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class DeterministicRunGeneratorTests
{
    [Fact]
    public void GenerateInitialRoom_ShouldCreateRoom_WithFourInitialNodes()
    {
        var generator = new DeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Depth.Should().Be(0);
        room.Theme.Should().Be("Threshold");
        room.Nodes.Should().HaveCount(4);
        room.Nodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public void GenerateInitialRoom_ShouldGenerateExpectedInitialEventTypes()
    {
        var generator = new DeterministicRunGenerator();

        var room = generator.GenerateInitialRoom("seed-test-001");

        room.Nodes.Select(node => node.EventType)
            .Should()
            .BeEquivalentTo(new[]
            {
                NodeEventType.Combat,
                NodeEventType.Memory,
                NodeEventType.Rest,
                NodeEventType.Item
            });
    }

    [Fact]
    public void GenerateInitialRoom_ShouldBeDeterministic_ForRiskAndRewardProfiles()
    {
        var generator = new DeterministicRunGenerator();

        var firstRoom = generator.GenerateInitialRoom("seed-test-001");
        var secondRoom = generator.GenerateInitialRoom("seed-test-001");

        var firstSnapshot = firstRoom.Nodes
            .Select(node => new
            {
                node.EventType,
                node.RiskLevel,
                node.RewardProfile
            })
            .ToArray();

        var secondSnapshot = secondRoom.Nodes
            .Select(node => new
            {
                node.EventType,
                node.RiskLevel,
                node.RewardProfile
            })
            .ToArray();

        secondSnapshot.Should().BeEquivalentTo(firstSnapshot, options => options.WithStrictOrdering());
    }
}