using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatRiskProfileResolverTests
{
    private static ICombatRiskProfileResolver CreateSut() => new CombatRiskProfileResolver();

    // -----------------------------------------------------------------------
    // Tier detection
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldReturnNormalTier_ForCombatNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, riskLevel: 2);
        profile.Tier.Should().Be(CombatTier.Normal);
    }

    [Fact]
    public void Resolve_ShouldReturnRareTier_ForRareNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.Rare, riskLevel: 3);
        profile.Tier.Should().Be(CombatTier.Rare);
    }

    [Fact]
    public void Resolve_ShouldReturnEliteTier_ForEliteNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, riskLevel: 3);
        profile.Tier.Should().Be(CombatTier.Elite);
    }

    [Fact]
    public void Resolve_ShouldReturnRoomBossTier_ForRoomBossNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.RoomBoss, riskLevel: 5);
        profile.Tier.Should().Be(CombatTier.RoomBoss);
    }

    [Fact]
    public void Resolve_ShouldReturnFinalBossTier_ForFinalBossNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.FinalBoss, riskLevel: 5);
        profile.Tier.Should().Be(CombatTier.FinalBoss);
    }

    // -----------------------------------------------------------------------
    // RiskTier passthrough
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1, RiskTier.Calme)]
    [InlineData(2, RiskTier.Tendu)]
    [InlineData(3, RiskTier.Dangereux)]
    [InlineData(4, RiskTier.Perilleux)]
    [InlineData(5, RiskTier.Fatal)]
    public void Resolve_ShouldExposeTheGivenRiskTier(int riskLevel, RiskTier expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, riskLevel);
        profile.RiskTier.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // DifficultyMultiplier — flat per-tier lookup, no more raw-delta formula
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1, 1.00)]
    [InlineData(2, 1.15)]
    [InlineData(3, 1.35)]
    [InlineData(4, 1.60)]
    [InlineData(5, 2.00)]
    public void Resolve_ShouldComputeDifficultyMultiplier_FromRiskTierAlone(int riskLevel, double expected)
    {
        // Same tier gives the same multiplier no matter the encounter type — the old
        // per-encounter-type BaseRisk/delta formula is gone.
        var profile = CreateSut().Resolve(NodeEventType.Combat, riskLevel);
        profile.DifficultyMultiplier.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(1, 1.00)]
    [InlineData(5, 1.75)]
    public void Resolve_ShouldComputeLootMultiplier_FromRiskTier(int riskLevel, double expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, riskLevel);
        profile.LootMultiplier.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(1, 1.00)]
    [InlineData(5, 1.50)]
    public void Resolve_ShouldComputeReputationMultiplier_FromRiskTier(int riskLevel, double expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Rare, riskLevel);
        profile.ReputationMultiplier.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 5)]
    public void Resolve_ShouldComputeEclatsBaseAmount_FromRiskTier(int riskLevel, int expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.RoomBoss, riskLevel);
        profile.EclatsBaseAmount.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Resolve_ShouldThrow_WhenRiskLevelIsOutOfRange(int riskLevel)
    {
        var sut = CreateSut();
        var act = () => sut.Resolve(NodeEventType.Combat, riskLevel);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // -----------------------------------------------------------------------
    // Rejection of non-combat node types
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(NodeEventType.Item)]
    [InlineData(NodeEventType.Rest)]
    [InlineData(NodeEventType.Npc)]
    [InlineData(NodeEventType.Law)]
    [InlineData(NodeEventType.Merchant)]
    [InlineData(NodeEventType.Curse)]
    [InlineData(NodeEventType.Memory)]
    public void Resolve_ShouldThrow_ForNonCombatNodeType(NodeEventType nonCombatType)
    {
        var sut = CreateSut();
        var act = () => sut.Resolve(nonCombatType, riskLevel: 3);
        act.Should().Throw<ArgumentException>(
            $"{nonCombatType} is not a combat type and must not be risk-scaled.");
    }

    [Theory]
    [InlineData(NodeEventType.Combat, true)]
    [InlineData(NodeEventType.Rare, true)]
    [InlineData(NodeEventType.Elite, true)]
    [InlineData(NodeEventType.RoomBoss, true)]
    [InlineData(NodeEventType.FinalBoss, true)]
    [InlineData(NodeEventType.Item, false)]
    [InlineData(NodeEventType.Rest, false)]
    [InlineData(NodeEventType.Npc, false)]
    [InlineData(NodeEventType.Law, false)]
    [InlineData(NodeEventType.Merchant, false)]
    [InlineData(NodeEventType.Curse, false)]
    [InlineData(NodeEventType.Memory, false)]
    public void IsCombatNodeType_ShouldReturnExpectedResult(NodeEventType eventType, bool expected)
    {
        var sut = CreateSut();
        sut.IsCombatNodeType(eventType).Should().Be(expected,
            $"{eventType} combat-type classification must be {expected}.");
    }
}
