using FluentValidation;

namespace Leds.GameEngine.Application.Runs.ChooseNode;

public sealed class ChooseNodeCommandValidator : AbstractValidator<ChooseNodeCommand>
{
    public ChooseNodeCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");

        RuleFor(command => command.NodeId)
            .NotEmpty()
            .WithMessage("Node id is required.");
    }
}