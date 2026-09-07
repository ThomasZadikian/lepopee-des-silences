using FluentValidation;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandValidator : AbstractValidator<StartRunCommand>
{
    public StartRunCommandValidator()
    {
        RuleFor(command => command.PlayerId)
            .NotEmpty()
            .WithMessage("Player id is required.");

        RuleFor(command => command.CharacterId)
            .Must(characterId => !characterId.HasValue || characterId.Value != Guid.Empty)
            .WithMessage("Character id must not be empty when provided.");
    }
}
