using FluentValidation;

namespace Leds.GameEngine.Application.Runs.ResolveSelectedNode;

public sealed class ResolveSelectedNodeCommandValidator : AbstractValidator<ResolveSelectedNodeCommand>
{
    public ResolveSelectedNodeCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}