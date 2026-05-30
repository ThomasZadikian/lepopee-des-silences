using FluentValidation;

namespace Leds.GameEngine.Application.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandValidator : AbstractValidator<ResolveCurrentEventCommand>
{
    public ResolveCurrentEventCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}