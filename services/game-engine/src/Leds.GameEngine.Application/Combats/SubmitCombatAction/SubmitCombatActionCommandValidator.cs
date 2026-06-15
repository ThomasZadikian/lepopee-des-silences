using FluentValidation;

namespace Leds.GameEngine.Application.Combats.SubmitCombatAction;

public sealed class SubmitCombatActionCommandValidator : AbstractValidator<SubmitCombatActionCommand>
{
    public SubmitCombatActionCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");

        RuleFor(command => command.CombatId)
            .NotEmpty()
            .WithMessage("Combat id is required.");

        RuleFor(command => command.ActorId)
            .NotEmpty()
            .WithMessage("Actor id is required.");

        RuleFor(command => command.TargetId)
            .NotEmpty()
            .WithMessage("Target id is required.");

        RuleFor(command => command.ActionType)
            .NotEmpty()
            .WithMessage("Action type is required.");
    }
}