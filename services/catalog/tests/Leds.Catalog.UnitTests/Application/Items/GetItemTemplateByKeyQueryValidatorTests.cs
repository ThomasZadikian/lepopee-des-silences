using FluentAssertions;
using Leds.Catalog.Application.Items.GetItemTemplateByKey;

namespace Leds.Catalog.UnitTests.Application.Items;

public sealed class GetItemTemplateByKeyQueryValidatorTests
{
    private readonly GetItemTemplateByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetItemTemplateByKeyQuery("item-memory-potion"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetItemTemplateByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Item template key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetItemTemplateByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Item template key must not exceed 128 characters.");
    }
}