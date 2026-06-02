using FluentAssertions;
using Leds.Catalog.Application.Skills.GetSkillTemplateByKey;

namespace Leds.Catalog.UnitTests.Skills;

public sealed class GetSkillTemplateByKeyQueryValidatorTests
{
    private readonly GetSkillTemplateByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetSkillTemplateByKeyQuery("skill-shadow-bite"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetSkillTemplateByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Skill template key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetSkillTemplateByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Skill template key must not exceed 128 characters.");
    }
}