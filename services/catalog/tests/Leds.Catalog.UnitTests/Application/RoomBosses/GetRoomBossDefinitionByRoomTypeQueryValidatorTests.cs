using FluentAssertions;
using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;

namespace Leds.Catalog.UnitTests.Application.RoomBosses;

public sealed class GetRoomBossDefinitionByRoomTypeQueryValidatorTests
{
    private readonly GetRoomBossDefinitionByRoomTypeQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenRoomTypeIsProvided()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByRoomTypeQuery("Threshold"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRoomTypeIsEmpty()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByRoomTypeQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room type is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRoomTypeIsWhitespace()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByRoomTypeQuery("   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room type is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenRoomTypeIsTooLong()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByRoomTypeQuery(new string('a', 65)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room type must not exceed 64 characters.");
    }
}
