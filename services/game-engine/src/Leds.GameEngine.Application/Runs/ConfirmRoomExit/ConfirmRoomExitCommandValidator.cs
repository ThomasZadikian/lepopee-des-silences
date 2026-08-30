using FluentValidation;

namespace Leds.GameEngine.Application.Runs.ConfirmRoomExit;

public sealed class ConfirmRoomExitCommandValidator : AbstractValidator<ConfirmRoomExitCommand>
{
    public ConfirmRoomExitCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");

        RuleFor(command => command.NodeId)
            .NotEmpty()
            .WithMessage("Node id is required.");
    }
}
