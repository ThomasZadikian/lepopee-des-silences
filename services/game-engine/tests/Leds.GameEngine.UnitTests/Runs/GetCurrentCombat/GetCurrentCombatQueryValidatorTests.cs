using FluentAssertions;
using Leds.GameEngine.Application.Runs.GetCurrentCombat;

namespace Leds.GameEngine.UnitTests.Runs.GetCurrentCombat;

public sealed class GetCurrentCombatQueryValidatorTests
{
    private readonly GetCurrentCombatQueryValidator _validator = new();

    private static readonly GetCurrentCombatQuery ValidQuery = new(Guid.NewGuid());

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var query = ValidQuery with { RunId = Guid.Empty };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenQueryIsValid()
    {
        var result = _validator.Validate(ValidQuery);

        result.IsValid.Should().BeTrue();
    }
}
