using FluentValidation;

namespace Leds.GameEngine.Application.Runs.ProgressRun;

public sealed class ProgressRunCommandValidator : AbstractValidator<ProgressRunCommand>
{
    public ProgressRunCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}