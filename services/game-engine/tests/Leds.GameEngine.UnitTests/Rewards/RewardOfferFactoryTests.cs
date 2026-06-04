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
}
