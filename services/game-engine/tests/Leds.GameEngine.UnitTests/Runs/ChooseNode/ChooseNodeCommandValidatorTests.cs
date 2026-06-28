using FluentAssertions;
using Leds.GameEngine.Application.Runs.ChooseNode;

namespace Leds.GameEngine.UnitTests.Runs.ChooseNode;

public sealed class ChooseNodeCommandValidatorTests
{
    private readonly ChooseNodeCommandValidator _validator = new();

    private static readonly ChooseNodeCommand ValidCommand = new(
        RunId: Guid.NewGuid(),
        NodeId: Guid.NewGuid());

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var command = ValidCommand with { RunId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldHaveError_WhenNodeIdIsEmpty()
    {
        var command = ValidCommand with { NodeId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Node id"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var result = _validator.Validate(ValidCommand);

        result.IsValid.Should().BeTrue();
    }
}
