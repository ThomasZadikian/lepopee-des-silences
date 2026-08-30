using FluentAssertions;
using Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.Items.Definitions;

public sealed class GetItemDefinitionByKeyQueryValidatorTests
{
    private readonly GetItemDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetItemDefinitionByKeyQuery("item.shadow-crystal"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsAtMaximumLength()
    {
        var result = _validator.Validate(
            new GetItemDefinitionByKeyQuery(new string('a', 160)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetItemDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetItemDefinitionByKeyQuery(new string('a', 161)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }
}
