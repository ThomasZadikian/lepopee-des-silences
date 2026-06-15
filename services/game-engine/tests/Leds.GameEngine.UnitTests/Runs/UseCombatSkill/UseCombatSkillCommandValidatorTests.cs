using FluentAssertions;
using Leds.GameEngine.Application.Runs.UseCombatSkill;

namespace Leds.GameEngine.UnitTests.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandValidatorTests
{
    private readonly UseCombatSkillCommandValidator _validator = new();

    private static readonly UseCombatSkillCommand ValidCommand = new(
        RunId: Guid.NewGuid(),
        CombatId: Guid.NewGuid(),
        ActorId: Guid.NewGuid(),
        SkillKey: "skill.basic.strike",
        TargetIds: [Guid.NewGuid()]);

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
    public void ShouldHaveError_WhenSkillKeyIsEmpty()
    {
        var command = ValidCommand with { SkillKey = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Skill key"));
    }

    [Fact]
    public void ShouldHaveError_WhenTargetIdsIsNull()
    {
        var command = ValidCommand with { TargetIds = null! };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Target ids"));
    }

    [Fact]
    public void ShouldHaveError_WhenTargetIdsIsEmpty()
    {
        var command = ValidCommand with { TargetIds = [] };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("target"));
    }

    [Fact]
    public void ShouldHaveError_WhenTargetIdsContainsDuplicates()
    {
        var targetId = Guid.NewGuid();
        var command = ValidCommand with { TargetIds = [targetId, targetId] };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("duplicate"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var result = _validator.Validate(ValidCommand);

        result.IsValid.Should().BeTrue();
    }
}