using FluentValidation;

namespace Leds.GameEngine.Application.Runs.AbandonRun;

public sealed class AbandonRunCommandValidator : AbstractValidator<AbandonRunCommand>
{
    public AbandonRunCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}