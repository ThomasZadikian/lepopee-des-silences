using FluentAssertions;
using Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.Curses;

public sealed class GetCurseDefinitionByKeyQueryValidatorTests
{
    private readonly GetCurseDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetCurseDefinitionByKeyQuery("curse-shadow-blight"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsAtMaximumLength()
    {
        var result = _validator.Validate(
            new GetCurseDefinitionByKeyQuery(new string('a', 160)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetCurseDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetCurseDefinitionByKeyQuery(new string('a', 161)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }
}
