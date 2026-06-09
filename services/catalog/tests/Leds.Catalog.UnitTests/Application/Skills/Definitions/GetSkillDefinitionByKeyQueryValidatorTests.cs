using FluentAssertions;
using Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class GetSkillDefinitionByKeyQueryValidatorTests
{
    private readonly GetSkillDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetSkillDefinitionByKeyQuery("skill.basic.strike"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetSkillDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Skill definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetSkillDefinitionByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Skill definition key must not exceed 128 characters.");
    }
}
