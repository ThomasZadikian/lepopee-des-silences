using FluentValidation;

namespace Leds.GameEngine.Application.Runs.GenerateNextNodes;

public sealed class GenerateNextNodesCommandValidator : AbstractValidator<GenerateNextNodesCommand>
{
    public GenerateNextNodesCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}