using FluentAssertions;
using Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;

namespace Leds.Catalog.UnitTests.Domain.EventTemplates;

public sealed class GetEventTemplateByKeyQueryValidatorTests
{
    private readonly GetEventTemplateByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetEventTemplateByKeyQuery("event-memory-threshold-v1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetEventTemplateByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Event template key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsWhitespace()
    {
        var result = _validator.Validate(
            new GetEventTemplateByKeyQuery("   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Event template key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetEventTemplateByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Event template key must not exceed 128 characters.");
    }
}