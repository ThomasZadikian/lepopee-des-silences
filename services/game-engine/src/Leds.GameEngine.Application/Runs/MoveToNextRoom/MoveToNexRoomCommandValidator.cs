using FluentValidation;

namespace Leds.GameEngine.Application.Runs.MoveToNextRoom;

public sealed class MoveToNextRoomCommandValidator : AbstractValidator<MoveToNextRoomCommand>
{
    public MoveToNextRoomCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}