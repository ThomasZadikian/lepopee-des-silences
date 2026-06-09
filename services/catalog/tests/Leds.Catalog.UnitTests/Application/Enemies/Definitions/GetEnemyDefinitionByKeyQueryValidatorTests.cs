using FluentAssertions;
using Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;

namespace Leds.Catalog.UnitTests.Application.Enemies.Definitions;

public sealed class GetEnemyDefinitionByKeyQueryValidatorTests
{
    private readonly GetEnemyDefinitionByKeyQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenKeyIsProvided()
    {
        var result = _validator.Validate(
            new GetEnemyDefinitionByKeyQuery("enemy.threshold.doubt-fragment"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsEmpty()
    {
        var result = _validator.Validate(
            new GetEnemyDefinitionByKeyQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Enemy definition key is required.");
    }

    [Fact]
    public void Validate_ShouldBeInvalid_WhenKeyIsTooLong()
    {
        var result = _validator.Validate(
            new GetEnemyDefinitionByKeyQuery(new string('a', 129)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Enemy definition key must not exceed 128 characters.");
    }
}
