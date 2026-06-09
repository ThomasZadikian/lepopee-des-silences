using FluentAssertions;
using Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;

namespace Leds.Catalog.UnitTests.Application.Enemies.Definitions;

public sealed class ListEnemyDefinitionsByRoomTypeQueryValidatorTests
{
    private readonly ListEnemyDefinitionsByRoomTypeQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenRoomTypeIsProvided()
    {
        var result = _validator.Validate(
            new ListEnemyDefinitionsByRoomTypeQuery("Threshold"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRoomTypeIsEmpty()
    {
        var result = _validator.Validate(
            new ListEnemyDefinitionsByRoomTypeQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Room type is required.");
    }
}
