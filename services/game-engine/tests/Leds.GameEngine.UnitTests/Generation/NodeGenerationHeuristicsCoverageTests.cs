using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;

namespace Leds.GameEngine.UnitTests.Generation;

public sealed class NodeGenerationHeuristicsCoverageTests
{
    [Theory]
    [InlineData(0, NodeEventType.Combat)]
    [InlineData(3, NodeEventType.Rest)]
    [InlineData(5, NodeEventType.Item)]
    public void PickWeightedNodeType_ShouldSelectExpectedBucket(int roll, NodeEventType expected)
    {
        NodeGenerationHeuristics.PickWeightedNodeType(Profile(), new FixedRandom(roll))
            .Should().Be(expected);
    }

    [Fact]
    public void PickRewardProfile_ShouldUseConfiguredSingleMultipleAndEmptyOptions()
    {
        var profile = new RoomTypeGenerationProfile(
            RoomType.Memory,
            [new NodeTypeWeight(NodeEventType.Item, 1)],
            0, 10,
            new Dictionary<NodeEventType, IReadOnlyList<string>>
            {
                [NodeEventType.Item] = ["one"],
                [NodeEventType.Rest] = ["a", "b"],
                [NodeEventType.Npc] = []
            });

        NodeGenerationHeuristics.PickRewardProfile(NodeEventType.Item, profile, new FixedRandom(0)).Should().Be("one");
        NodeGenerationHeuristics.PickRewardProfile(NodeEventType.Rest, profile, new FixedRandom(1)).Should().Be("b");
        NodeGenerationHeuristics.PickRewardProfile(NodeEventType.Npc, profile, new FixedRandom(0)).Should().Be("narrative");
    }

    [Theory]
    [InlineData(NodeEventType.Combat, "combat-common")]
    [InlineData(NodeEventType.Elite, "elite")]
    [InlineData(NodeEventType.Rest, "rest-safe")]
    [InlineData(NodeEventType.Item, "item-common")]
    [InlineData(NodeEventType.Npc, "narrative")]
    [InlineData(NodeEventType.Merchant, "merchant")]
    [InlineData(NodeEventType.Law, "law")]
    [InlineData(NodeEventType.Curse, "curse")]
    [InlineData(NodeEventType.Rare, "rare")]
    [InlineData(NodeEventType.FinalBoss, "standard")]
    public void PickRewardProfile_ShouldCoverFallbackSwitch(NodeEventType type, string expected)
    {
        NodeGenerationHeuristics.PickRewardProfile(type, Profile(), new FixedRandom(0))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(NodeEventType.Combat, 0, RiskTier.Calme)]
    [InlineData(NodeEventType.Combat, 100, RiskTier.Fatal)]
    [InlineData(NodeEventType.Elite, 40, RiskTier.Dangereux)]
    public void DeriveCombatRiskTier_ShouldMapCombatRisk(NodeEventType type, int risk, RiskTier expected)
    {
        NodeGenerationHeuristics.DeriveCombatRiskTier(type, risk).Should().Be(expected);
    }

    [Fact]
    public void DeriveCombatRiskTier_ShouldReturnNullForNonCombatNode()
    {
        NodeGenerationHeuristics.DeriveCombatRiskTier(NodeEventType.Item, 50).Should().BeNull();
    }

    private static RoomTypeGenerationProfile Profile() => new(
        RoomType.Memory,
        [
            new NodeTypeWeight(NodeEventType.Combat, 3),
            new NodeTypeWeight(NodeEventType.Rest, 2),
            new NodeTypeWeight(NodeEventType.Item, 1)
        ],
        0, 100);

    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => Math.Clamp(value, 0, maxValue - 1);
    }
}
