using FluentAssertions;
using Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;

namespace Leds.Catalog.UnitTests.Application.Skills.Definitions;

public sealed class ListSkillDefinitionsByTypeQueryValidatorTests
{
    private readonly ListSkillDefinitionsByTypeQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenSkillTypeIsProvided()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByTypeQuery("Damage"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenSkillTypeIsEmpty()
    {
        var result = _validator.Validate(
            new ListSkillDefinitionsByTypeQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Skill type is required.");
    }
}
