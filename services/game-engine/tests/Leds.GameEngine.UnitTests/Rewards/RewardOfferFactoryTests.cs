using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardOfferFactoryTests
{
    private static RewardOfferFactory CreateFactory() =>
        new(new CombatRiskProfileResolver());

    // -----------------------------------------------------------------------
    // Basic structural tests (pre-existing, updated for new signature)
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateCombatRewardOffer_ShouldReturnOfferWithThreeChoices()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 25);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.State.Should().Be(RewardOfferState.Pending);
        offer.Choices.Should().HaveCount(3);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldIncludeHealChoice()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 25);

        offer.Choices.Should().Contain(choice => choice.RewardType == RewardType.Heal);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldExposeHealAndItemChoices()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 50);

        offer.Choices.Should().Contain(choice =>
            choice.RewardType == RewardType.Heal ||
            choice.RewardType == RewardType.TemporaryItem);
    }

    // -----------------------------------------------------------------------
    // Tier-aware reward profiles (pre-existing)
    // -----------------------------------------------------------------------

    [Fact]
    public void CombatOutcome_ShouldUseNormalRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 30);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "MVP combat rewards expose heal and item choices.");
    }

    [Fact]
    public void RareCombatOutcome_ShouldUseRareRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Rare, NodeEventType.Rare, riskLevel: 50);

        offer.Source.Should().Be(RewardSource.Rare);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Rare rewards expose heal and item choices.");
    }

    [Fact]
    public void EliteCombatOutcome_ShouldUseEliteRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 65);

        offer.Source.Should().Be(RewardSource.Elite);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Elite rewards expose heal and item choices.");
    }

    [Fact]
    public void RoomBossOutcome_ShouldUseBossRewardProfile()
    {
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.RoomBoss, NodeEventType.RoomBoss, riskLevel: 85);

        offer.Source.Should().Be(RewardSource.RoomBoss);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal || c.RewardType == RewardType.TemporaryItem,
            because: "Boss rewards expose heal and item choices.");
    }

    // -----------------------------------------------------------------------
    // CombatScaling metadata — Normal combat
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForNormalCombat()
    {
        // Combat baseRisk 20, actualRisk 20 → delta 0, multiplier 1.00
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 20);

        offer.CombatScaling.Should().NotBeNull(
            "CombatScaling must be populated for all combat offers.");

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Normal);
        scaling.BaseRisk.Should().Be(20);
        scaling.ActualRisk.Should().Be(20);
        scaling.RiskDelta.Should().Be(0);
        scaling.RewardPowerMultiplier.Should().BeApproximately(1.0, 0.001,
            "no delta → multiplier is exactly 1.00.");
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForRareCombat()
    {
        // Rare baseRisk 30, actualRisk 70 → delta 40, multiplier 1.40
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Rare, NodeEventType.Rare, riskLevel: 70);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Rare);
        scaling.BaseRisk.Should().Be(30);
        scaling.RiskDelta.Should().Be(40);
        scaling.RewardPowerMultiplier.Should().BeApproximately(1.40, 0.001,
            "Rare risk 70: delta 40, multiplier 1.40.");
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForEliteCombat()
    {
        // Elite baseRisk 35, actualRisk 75 → delta 40, multiplier 1.40
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 75);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.Elite);
        scaling.BaseRisk.Should().Be(35);
        scaling.RiskDelta.Should().Be(40);
        scaling.RewardPowerMultiplier.Should().BeApproximately(1.40, 0.001,
            "Elite risk 75: delta 40, multiplier 1.40.");
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeRiskScalingMetadata_ForRoomBossCombat()
    {
        // RoomBoss baseRisk 50, actualRisk 90 → delta 40, multiplier 1.40
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.RoomBoss, NodeEventType.RoomBoss, riskLevel: 90);

        var scaling = offer.CombatScaling!;
        scaling.Tier.Should().Be(CombatTier.RoomBoss);
        scaling.BaseRisk.Should().Be(50);
        scaling.RiskDelta.Should().Be(40);
        scaling.RewardPowerMultiplier.Should().BeApproximately(1.40, 0.001,
            "RoomBoss risk 90: delta 40, multiplier 1.40.");
    }

    [Fact]
    public void CreateRewardOffer_ShouldHaveMultiplierOne_WhenRiskEqualsBaseRisk()
    {
        // Elite at exactly its base risk — no bonus
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Elite, NodeEventType.Elite, riskLevel: 35);

        offer.CombatScaling!.RewardPowerMultiplier.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CreateRewardOffer_ShouldClampMultiplier_WhenRiskIsMaximum()
    {
        // Normal baseRisk 20, actualRisk 100 → delta 80 → raw 1.80 → clamped 1.75
        var offer = CreateFactory()
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: 100);

        offer.CombatScaling!.RewardPowerMultiplier.Should().BeApproximately(1.75, 0.001,
            "multiplier is clamped at 1.75.");
    }

    [Fact]
    public void CreateRewardOffer_ShouldIncludeCorrectRiskBand_InScaling()
    {
        var lowOffer      = CreateFactory().CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, 10);
        var moderateOffer = CreateFactory().CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, 35);
        var highOffer     = CreateFactory().CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, 60);
        var criticalOffer = CreateFactory().CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, 80);

        lowOffer.CombatScaling!.RiskBand.Should().Be(RiskBand.Low);
        moderateOffer.CombatScaling!.RiskBand.Should().Be(RiskBand.Moderate);
        highOffer.CombatScaling!.RiskBand.Should().Be(RiskBand.High);
        criticalOffer.CombatScaling!.RiskBand.Should().Be(RiskBand.Critical);
    }
}
