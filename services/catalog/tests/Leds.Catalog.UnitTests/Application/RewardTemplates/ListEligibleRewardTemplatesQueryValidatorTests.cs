using FluentAssertions;
using Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;

namespace Leds.Catalog.UnitTests.Application.RewardTemplates;

public sealed class ListEligibleRewardTemplatesQueryValidatorTests
{
    private readonly ListEligibleRewardTemplatesQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenSourceTypeIsProvided()
    {
        var result = _validator.Validate(
            new ListEligibleRewardTemplatesQuery(
                SourceType: "enemy",
                Depth: 1,
                CombatTier: "normal",
                DifficultyMultiplier: 1.0,
                RewardPowerMultiplier: 1.0));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenSourceTypeIsAtMaximumLength()
    {
        var result = _validator.Validate(
            new ListEligibleRewardTemplatesQuery(
                SourceType: new string('a', 80),
                Depth: null,
                CombatTier: null,
                DifficultyMultiplier: null,
                RewardPowerMultiplier: null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenSourceTypeIsEmpty()
    {
        var result = _validator.Validate(
            new ListEligibleRewardTemplatesQuery(
                SourceType: string.Empty,
                Depth: null,
                CombatTier: null,
                DifficultyMultiplier: null,
                RewardPowerMultiplier: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceType");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenSourceTypeIsTooLong()
    {
        var result = _validator.Validate(
            new ListEligibleRewardTemplatesQuery(
                SourceType: new string('a', 81),
                Depth: null,
                CombatTier: null,
                DifficultyMultiplier: null,
                RewardPowerMultiplier: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceType");
    }
}
