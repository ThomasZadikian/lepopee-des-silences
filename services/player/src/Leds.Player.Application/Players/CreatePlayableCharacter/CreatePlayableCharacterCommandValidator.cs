using FluentValidation;

namespace Leds.Player.Application.Players.CreatePlayableCharacter;

public sealed class CreatePlayableCharacterCommandValidator : AbstractValidator<CreatePlayableCharacterCommand>
{
    public CreatePlayableCharacterCommandValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(40);
        RuleFor(x => x.ArchetypeKey)
            .NotEmpty()
            .Must(key => string.Equals(key, "archetype.porteur", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Unknown or unavailable archetype.");
    }
}
