using FluentValidation;

namespace Leds.GameEngine.Application.Rewards.SelectReward;

public sealed class SelectRewardCommandValidator : AbstractValidator<SelectRewardCommand>
{
    public SelectRewardCommandValidator()
    {
        RuleFor(cmd => cmd.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");

        RuleFor(cmd => cmd.ChoiceId)
            .NotEmpty()
            .WithMessage("Choice id is required.");
    }
}
