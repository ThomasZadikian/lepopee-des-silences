using FluentAssertions;
using Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.PalaceLaws;

public sealed class GetPalaceLawDefinitionByKeyQueryValidatorTests
{
    private readonly GetPalaceLawDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetPalaceLawDefinitionByKeyQuery("law-silence-v1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetPalaceLawDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Palace law definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsWhitespace()
    {
        var result = _validator.Validate(
            new GetPalaceLawDefinitionByKeyQuery("   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Palace law definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetPalaceLawDefinitionByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Palace law definition key must not exceed 128 characters.");
    }
}