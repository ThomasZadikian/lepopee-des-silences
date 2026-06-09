using FluentValidation;

namespace Leds.GameEngine.Application.Runs.SaveAndExitRun;

public sealed class SaveAndExitRunCommandValidator : AbstractValidator<SaveAndExitRunCommand>
{
    public SaveAndExitRunCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}
