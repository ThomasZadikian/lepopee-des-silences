using FluentAssertions;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;

namespace Leds.GameEngine.UnitTests.Combats.SubmitCombatAction;

public sealed class SubmitCombatActionCommandValidatorTests
{
    private readonly SubmitCombatActionCommandValidator _validator = new();

    private static readonly SubmitCombatActionCommand ValidCommand = new(
        RunId: Guid.NewGuid(),
        CombatId: Guid.NewGuid(),
        ActorId: Guid.NewGuid(),
        TargetId: Guid.NewGuid(),
        ActionType: "Skill");

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var command = ValidCommand with { RunId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldHaveError_WhenCombatIdIsEmpty()
    {
        var command = ValidCommand with { CombatId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Combat id"));
    }

    [Fact]
    public void ShouldHaveError_WhenActorIdIsEmpty()
    {
        var command = ValidCommand with { ActorId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Actor id"));
    }

    [Fact]
    public void ShouldHaveError_WhenTargetIdIsEmpty()
    {
        var command = ValidCommand with { TargetId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Target id"));
    }

    [Fact]
    public void ShouldHaveError_WhenActionTypeIsEmpty()
    {
        var command = ValidCommand with { ActionType = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Action type"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var result = _validator.Validate(ValidCommand);

        result.IsValid.Should().BeTrue();
    }
}
