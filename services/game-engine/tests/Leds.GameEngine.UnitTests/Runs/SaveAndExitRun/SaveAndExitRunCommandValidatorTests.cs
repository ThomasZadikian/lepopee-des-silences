using FluentAssertions;
using Leds.GameEngine.Application.Runs.SaveAndExitRun;

namespace Leds.GameEngine.UnitTests.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandValidatorTests
{
    private readonly SaveAndExitRunCommandValidator _validator = new();

    private static readonly SaveAndExitRunCommand ValidCommand = new(Guid.NewGuid());

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
