using FluentValidation;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandValidator : AbstractValidator<StartRunCommand>
{
    public StartRunCommandValidator()
    {
        RuleFor(command => command.PlayerId)
            .NotEmpty()
            .WithMessage("Player id is required.");
    }
}