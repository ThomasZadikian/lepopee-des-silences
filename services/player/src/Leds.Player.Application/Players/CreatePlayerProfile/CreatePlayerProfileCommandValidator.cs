using FluentValidation;

namespace Leds.Player.Application.Players.CreatePlayerProfile;

public sealed class CreatePlayerProfileCommandValidator : AbstractValidator<CreatePlayerProfileCommand>
{
    public CreatePlayerProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Player display name is required.")
            .MaximumLength(128)
            .WithMessage("Player display name must not exceed 128 characters.");
    }
}
