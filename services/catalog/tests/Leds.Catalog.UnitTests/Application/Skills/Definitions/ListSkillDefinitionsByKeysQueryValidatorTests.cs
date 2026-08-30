using FluentAssertions;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class ListSkillDefinitionsByKeysQueryValidatorTests
{
    private readonly ListSkillDefinitionsByKeysQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeysAreProvided()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByKeysQuery(["skill.basic.strike"]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeysIsNull()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByKeysQuery(null!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Skill definition keys collection is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeysIsEmpty()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByKeysQuery([]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "At least one skill definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByKeysQuery([""]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Each skill definition key must not be empty.");
    }
}
