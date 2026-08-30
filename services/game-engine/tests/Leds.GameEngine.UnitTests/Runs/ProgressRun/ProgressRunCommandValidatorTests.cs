using FluentAssertions;
using Leds.GameEngine.Application.Runs.ProgressRun;

namespace Leds.GameEngine.UnitTests.Runs.ProgressRun;

public sealed class ProgressRunCommandValidatorTests
{
    private readonly ProgressRunCommandValidator _validator = new();

    private static readonly ProgressRunCommand ValidCommand = new(Guid.NewGuid());

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
