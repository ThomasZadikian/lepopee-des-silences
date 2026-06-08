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
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRiskLevel: 20);
        profile.Tier.Should().Be(CombatTier.Normal);
    }

    [Fact]
    public void Resolve_ShouldReturnRareTier_ForRareNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.Rare, actualRiskLevel: 30);
        profile.Tier.Should().Be(CombatTier.Rare);
    }

    [Fact]
    public void Resolve_ShouldReturnEliteTier_ForEliteNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 35);
        profile.Tier.Should().Be(CombatTier.Elite);
    }

    [Fact]
    public void Resolve_ShouldReturnRoomBossTier_ForRoomBossNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.RoomBoss, actualRiskLevel: 85);
        profile.Tier.Should().Be(CombatTier.RoomBoss);
    }

    [Fact]
    public void Resolve_ShouldReturnFinalBossTier_ForFinalBossNode()
    {
        var profile = CreateSut().Resolve(NodeEventType.FinalBoss, actualRiskLevel: 90);
        profile.Tier.Should().Be(CombatTier.FinalBoss);
    }

    // -----------------------------------------------------------------------
    // BaseRisk values
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldUseExpectedBaseRisk_ForNormalCombat()
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRiskLevel: 20);
        profile.BaseRisk.Should().Be(20, "Normal combat base risk is 20.");
    }

    [Fact]
    public void Resolve_ShouldUseExpectedBaseRisk_ForRareCombat()
    {
        var profile = CreateSut().Resolve(NodeEventType.Rare, actualRiskLevel: 30);
        profile.BaseRisk.Should().Be(30, "Rare combat base risk is 30.");
    }

    [Fact]
    public void Resolve_ShouldUseExpectedBaseRisk_ForEliteCombat()
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 35);
        profile.BaseRisk.Should().Be(35, "Elite combat base risk is 35.");
    }

    [Fact]
    public void Resolve_ShouldUseExpectedBaseRisk_ForRoomBoss()
    {
        var profile = CreateSut().Resolve(NodeEventType.RoomBoss, actualRiskLevel: 85);
        profile.BaseRisk.Should().Be(50, "RoomBoss base risk is 50.");
    }

    [Fact]
    public void Resolve_ShouldUseExpectedBaseRisk_ForFinalBoss()
    {
        var profile = CreateSut().Resolve(NodeEventType.FinalBoss, actualRiskLevel: 90);
        profile.BaseRisk.Should().Be(70, "FinalBoss base risk is 70.");
    }

    // -----------------------------------------------------------------------
    // RiskDelta calculation
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldComputeZeroDelta_WhenActualRiskEqualsBaseRisk()
    {
        // Elite: baseRisk 35, actualRisk 35 → delta 0
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 35);
        profile.RiskDelta.Should().Be(0, "delta is zero when actual equals base.");
    }

    [Fact]
    public void Resolve_ShouldComputePositiveDelta_WhenActualRiskExceedsBaseRisk()
    {
        // Elite: baseRisk 35, actualRisk 75 → delta 40
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 75);
        profile.RiskDelta.Should().Be(40, "delta = actualRisk − baseRisk = 75 − 35 = 40.");
    }

    [Fact]
    public void Resolve_ShouldNotComputeNegativeDelta_WhenActualRiskIsBelowBaseRisk()
    {
        // Elite: baseRisk 35, actualRisk 10 → delta must be clamped to 0
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 10);
        profile.RiskDelta.Should().Be(0, "negative deltas are clamped to zero.");
    }

    // -----------------------------------------------------------------------
    // DifficultyMultiplier
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldComputeMultiplierOfOne_WhenDeltaIsZero()
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 35);
        profile.DifficultyMultiplier.Should().BeApproximately(1.0, 0.001,
            "1.0 + 0/100 = 1.0 when delta is zero.");
    }

    [Fact]
    public void Resolve_ShouldComputeDifficultyMultiplier_FromRiskDelta()
    {
        // Elite baseRisk 35, actualRisk 75 → delta 40 → multiplier 1.40
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 75);
        profile.DifficultyMultiplier.Should().BeApproximately(1.40, 0.001,
            "1.0 + 40/100 = 1.40.");
    }

    [Fact]
    public void Resolve_ShouldComputeRewardPowerMultiplier_FromRiskDelta()
    {
        // Same formula as difficulty multiplier
        var profile = CreateSut().Resolve(NodeEventType.Rare, actualRiskLevel: 70);
        // Rare baseRisk 30, delta 40, multiplier 1.40
        profile.RewardPowerMultiplier.Should().BeApproximately(1.40, 0.001,
            "rewardPowerMultiplier equals difficultyMultiplier.");
        profile.RewardPowerMultiplier.Should().Be(profile.DifficultyMultiplier);
    }

    [Fact]
    public void Resolve_ShouldClampMultiplier_ToConfiguredMaximum()
    {
        // Normal baseRisk 20, actualRisk 100 → delta 80 → raw 1.80 → clamped to 1.75
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRiskLevel: 100);
        profile.DifficultyMultiplier.Should().BeApproximately(1.75, 0.001,
            "multiplier is clamped at 1.75 regardless of raw calculation.");
        profile.RewardPowerMultiplier.Should().BeApproximately(1.75, 0.001);
    }

    [Fact]
    public void Resolve_ShouldComputeMultiplier_ForRoomBoss_WithHighRisk()
    {
        // RoomBoss baseRisk 50, actualRisk 90 → delta 40 → multiplier 1.40
        var profile = CreateSut().Resolve(NodeEventType.RoomBoss, actualRiskLevel: 90);
        profile.RiskDelta.Should().Be(40);
        profile.DifficultyMultiplier.Should().BeApproximately(1.40, 0.001);
    }

    // -----------------------------------------------------------------------
    // RiskBand mapping
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0,  RiskBand.Low)]
    [InlineData(10, RiskBand.Low)]
    [InlineData(24, RiskBand.Low)]
    public void Resolve_ShouldReturnLowRiskBand(int actualRisk, RiskBand expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRisk);
        profile.RiskBand.Should().Be(expected, $"actualRisk {actualRisk} falls in the Low band (0–24).");
    }

    [Theory]
    [InlineData(25, RiskBand.Moderate)]
    [InlineData(35, RiskBand.Moderate)]
    [InlineData(49, RiskBand.Moderate)]
    public void Resolve_ShouldReturnModerateRiskBand(int actualRisk, RiskBand expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRisk);
        profile.RiskBand.Should().Be(expected, $"actualRisk {actualRisk} falls in the Moderate band (25–49).");
    }

    [Theory]
    [InlineData(50, RiskBand.High)]
    [InlineData(65, RiskBand.High)]
    [InlineData(74, RiskBand.High)]
    public void Resolve_ShouldReturnHighRiskBand(int actualRisk, RiskBand expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRisk);
        profile.RiskBand.Should().Be(expected, $"actualRisk {actualRisk} falls in the High band (50–74).");
    }

    [Theory]
    [InlineData(75,  RiskBand.Critical)]
    [InlineData(90,  RiskBand.Critical)]
    [InlineData(100, RiskBand.Critical)]
    public void Resolve_ShouldReturnCriticalRiskBand(int actualRisk, RiskBand expected)
    {
        var profile = CreateSut().Resolve(NodeEventType.Combat, actualRisk);
        profile.RiskBand.Should().Be(expected, $"actualRisk {actualRisk} falls in the Critical band (75–100).");
    }

    // -----------------------------------------------------------------------
    // ActualRisk passthrough
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_ShouldPreserveActualRisk()
    {
        var profile = CreateSut().Resolve(NodeEventType.Elite, actualRiskLevel: 72);
        profile.ActualRisk.Should().Be(72);
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
        var act = () => sut.Resolve(nonCombatType, actualRiskLevel: 50);
        act.Should().Throw<ArgumentException>(
            $"{nonCombatType} is not a combat type and must not be risk-scaled.");
    }

    [Theory]
    [InlineData(NodeEventType.Combat,   true)]
    [InlineData(NodeEventType.Rare,     true)]
    [InlineData(NodeEventType.Elite,    true)]
    [InlineData(NodeEventType.RoomBoss, true)]
    [InlineData(NodeEventType.FinalBoss,true)]
    [InlineData(NodeEventType.Item,     false)]
    [InlineData(NodeEventType.Rest,     false)]
    [InlineData(NodeEventType.Npc,      false)]
    [InlineData(NodeEventType.Law,      false)]
    [InlineData(NodeEventType.Merchant, false)]
    [InlineData(NodeEventType.Curse,    false)]
    [InlineData(NodeEventType.Memory,   false)]
    public void IsCombatNodeType_ShouldReturnExpectedResult(NodeEventType eventType, bool expected)
    {
        var sut = CreateSut();
        sut.IsCombatNodeType(eventType).Should().Be(expected,
            $"{eventType} combat-type classification must be {expected}.");
    }
}
