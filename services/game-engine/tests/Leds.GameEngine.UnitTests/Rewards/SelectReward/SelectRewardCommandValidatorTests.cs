using FluentAssertions;
using Leds.GameEngine.Application.Rewards.SelectReward;

namespace Leds.GameEngine.UnitTests.Rewards.SelectReward;

public sealed class SelectRewardCommandValidatorTests
{
    private readonly SelectRewardCommandValidator _validator = new();

    private static readonly SelectRewardCommand ValidCommand = new(
        RunId: Guid.NewGuid(),
        ChoiceId: Guid.NewGuid());

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var command = ValidCommand with { RunId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldHaveError_WhenChoiceIdIsEmpty()
    {
        var command = ValidCommand with { ChoiceId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Choice id"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenCommandIsValid()
    {
        var result = _validator.Validate(ValidCommand);

        result.IsValid.Should().BeTrue();
    }
}
