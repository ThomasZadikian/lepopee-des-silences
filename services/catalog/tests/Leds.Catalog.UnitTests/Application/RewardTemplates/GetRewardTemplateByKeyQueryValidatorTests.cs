using FluentAssertions;
using Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;

namespace Leds.Catalog.UnitTests.Application.RewardTemplates;

public sealed class GetRewardTemplateByKeyQueryValidatorTests
{
    private readonly GetRewardTemplateByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetRewardTemplateByKeyQuery("reward.healing-pool"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsAtMaximumLength()
    {
        var result = _validator.Validate(
            new GetRewardTemplateByKeyQuery(new string('a', 160)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetRewardTemplateByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetRewardTemplateByKeyQuery(new string('a', 161)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }
}
