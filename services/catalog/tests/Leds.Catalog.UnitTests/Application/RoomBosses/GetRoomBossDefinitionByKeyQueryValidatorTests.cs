using FluentAssertions;
using Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.RoomBosses;

public sealed class GetRoomBossDefinitionByKeyQueryValidatorTests
{
    private readonly GetRoomBossDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByKeyQuery("boss.threshold.warden"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room boss definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsWhitespace()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByKeyQuery("   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room boss definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetRoomBossDefinitionByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Room boss definition key must not exceed 128 characters.");
    }
}
