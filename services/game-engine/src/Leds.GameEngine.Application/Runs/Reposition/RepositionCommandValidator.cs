using FluentValidation;

namespace Leds.GameEngine.Application.Runs.Reposition;

public sealed class RepositionCommandValidator : AbstractValidator<RepositionCommand>
{
    public RepositionCommandValidator()
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

        RuleFor(command => command.Row)
            .NotEmpty()
            .WithMessage("Row is required.")
            .Must(row => Enum.TryParse<Domain.Combats.CombatRow>(row, ignoreCase: true, out _))
            .WithMessage("Row must be 'Front' or 'Back'.");
    }
}
