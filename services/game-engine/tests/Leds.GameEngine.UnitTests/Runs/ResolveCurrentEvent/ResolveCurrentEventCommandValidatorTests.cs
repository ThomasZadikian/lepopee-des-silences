using FluentAssertions;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

namespace Leds.GameEngine.UnitTests.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandValidatorTests
{
    private readonly ResolveCurrentEventCommandValidator _validator = new();

    private static readonly ResolveCurrentEventCommand ValidCommand = new(Guid.NewGuid());

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var command = ValidCommand with { RunId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var result = _validator.Validate(ValidCommand);

        result.IsValid.Should().BeTrue();
    }
}
