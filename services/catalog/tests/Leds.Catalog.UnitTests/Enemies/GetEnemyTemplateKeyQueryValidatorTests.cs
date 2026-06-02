using FluentAssertions;
using Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;

namespace Leds.Catalog.UnitTests.Enemies;

public sealed class GetEnemyTemplateByKeyQueryValidatorTests
{
    private readonly GetEnemyTemplateByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetEnemyTemplateByKeyQuery("enemy-shadow-wolf"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetEnemyTemplateByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Enemy template key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetEnemyTemplateByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Enemy template key must not exceed 128 characters.");
    }
}