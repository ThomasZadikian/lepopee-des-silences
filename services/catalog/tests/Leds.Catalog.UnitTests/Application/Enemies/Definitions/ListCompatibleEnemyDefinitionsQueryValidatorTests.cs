using FluentAssertions;
using Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;

namespace Leds.Catalog.UnitTests.Application.Enemies.Definitions;

public sealed class ListCompatibleEnemyDefinitionsQueryValidatorTests
{
    private readonly ListCompatibleEnemyDefinitionsQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenInputIsValid()
    {
        var result = _validator.Validate(
            new ListCompatibleEnemyDefinitionsQuery("Threshold", 3));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRoomTypeIsEmpty()
    {
        var result = _validator.Validate(
            new ListCompatibleEnemyDefinitionsQuery(string.Empty, 3));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Room type is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRiskLevelIsZero()
    {
        var result = _validator.Validate(
            new ListCompatibleEnemyDefinitionsQuery("Threshold", 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Risk level must be at least 1.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRiskLevelExceedsFive()
    {
        var result = _validator.Validate(
            new ListCompatibleEnemyDefinitionsQuery("Threshold", 6));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Risk level must not exceed 5.");
    }
}
