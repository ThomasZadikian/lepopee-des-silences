using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardOfferTests
{
    [Fact]
    public void Create_ShouldCreateRewardOfferWithChoices()
    {
        var choices = new[]
        {
            RewardChoice.Create(RewardType.Heal, "Heal", "Restore some HP.", "heal:10"),
            RewardChoice.Create(RewardType.StatBonus, "Attack Up", "Boost attack.", "stat_bonus:attack:3")
        };

        var offer = RewardOffer.Create(RewardSource.Combat, choices);

        offer.Id.Value.Should().NotBeEmpty();
        offer.Source.Should().Be(RewardSource.Combat);
        offer.State.Should().Be(RewardOfferState.Pending);
        offer.Choices.Should().HaveCount(2);
        offer.SelectedChoiceId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoChoices()
    {
        var act = () => RewardOffer.Create(
            RewardSource.Combat,
            Array.Empty<RewardChoice>());

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Reward offer must have at least one choice.");
    }

    [Fact]
    public void SelectChoice_ShouldTransitionToSelected()
    {
        var choices = new[]
        {
            RewardChoice.Create(RewardType.Heal, "Heal", "Restore some HP.", "heal:10"),
            RewardChoice.Create(RewardType.StatBonus, "Attack Up", "Boost attack.", "stat_bonus:attack:3")
        };

        var offer = RewardOffer.Create(RewardSource.Combat, choices);

        offer.SelectChoice(choices[0].Id);

        offer.State.Should().Be(RewardOfferState.Selected);
        offer.SelectedChoiceId.Should().Be(choices[0].Id);
    }

    [Fact]
    public void SelectChoice_ShouldThrow_WhenOfferIsAlreadySelected()
    {
        var choices = new[]
        {
            RewardChoice.Create(RewardType.Heal, "Heal", "Restore some HP.", "heal:10")
        };

        var offer = RewardOffer.Create(RewardSource.Combat, choices);
        offer.SelectChoice(choices[0].Id);

        var act = () => offer.SelectChoice(choices[0].Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a pending reward offer can be selected.");
    }

    [Fact]
    public void SelectChoice_ShouldThrow_WhenChoiceDoesNotExist()
    {
        var choices = new[]
        {
            RewardChoice.Create(RewardType.Heal, "Heal", "Restore some HP.", "heal:10")
        };

        var offer = RewardOffer.Create(RewardSource.Combat, choices);
        var unknownChoiceId = RewardChoiceId.New();

        var act = () => offer.SelectChoice(unknownChoiceId);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Reward choice was not found in the offer.");
    }
}