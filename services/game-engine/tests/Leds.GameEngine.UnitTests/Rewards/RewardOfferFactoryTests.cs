using FluentAssertions;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardOfferFactoryTests
{
    private readonly RewardOfferFactory _factory = new();

    [Fact]
    public void CreateCombatRewardOffer_ShouldReturnOfferWithThreeChoices()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Combat, riskLevel: 25);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.State.Should().Be(RewardOfferState.Pending);
        offer.Choices.Should().HaveCount(3);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldIncludeHealChoice()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Combat, riskLevel: 25);

        offer.Choices.Should().Contain(choice =>
            choice.RewardType == RewardType.Heal);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldIncludeStatBonusChoice()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Elite, riskLevel: 50);

        offer.Choices.Should().Contain(choice =>
            choice.RewardType == RewardType.StatBonus);
    }

    [Fact]
    public void CreateCombatRewardOffer_ShouldIncludeMemoryFragmentChoice()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.RoomBoss, riskLevel: 85);

        offer.Choices.Should().Contain(choice =>
            choice.RewardType == RewardType.MemoryFragment);
    }

    // ---------------------------------------------------------------------------
    // Tier-aware reward profiles
    // ---------------------------------------------------------------------------

    [Fact]
    public void CombatOutcome_ShouldUseNormalRewardProfile()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Combat, riskLevel: 30);

        offer.Source.Should().Be(RewardSource.Combat);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.MemoryFragment &&
            c.PayloadKey.Contains("common"),
            because: "Normal combat rewards memory_fragment:common.");
    }

    [Fact]
    public void RareCombatOutcome_ShouldUseRareRewardProfile()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Rare, riskLevel: 50);

        offer.Source.Should().Be(RewardSource.Rare);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.MemoryFragment &&
            c.PayloadKey.Contains("rare"),
            because: "Rare combat rewards memory_fragment:rare.");
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.StatBonus &&
            c.PayloadKey.Contains("5"),
            because: "Rare combat stat bonus should be +5.");
    }

    [Fact]
    public void EliteCombatOutcome_ShouldUseEliteRewardProfile()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.Elite, riskLevel: 65);

        offer.Source.Should().Be(RewardSource.Elite);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.MemoryFragment &&
            c.PayloadKey.Contains("elite"),
            because: "Elite combat rewards memory_fragment:elite.");
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.StatBonus &&
            c.PayloadKey.Contains("defense"),
            because: "Elite combat stat bonus targets defense.");
    }

    [Fact]
    public void RoomBossOutcome_ShouldUseBossRewardProfile()
    {
        var offer = _factory.CreateCombatRewardOffer(RewardSource.RoomBoss, riskLevel: 85);

        offer.Source.Should().Be(RewardSource.RoomBoss);
        offer.Choices.Should().HaveCount(3);
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.MemoryFragment &&
            c.PayloadKey.Contains("boss"),
            because: "Boss rewards memory_fragment:boss.");
        offer.Choices.Should().Contain(c =>
            c.RewardType == RewardType.Heal,
            because: "Boss reward must include a major heal choice.");
    }
}
