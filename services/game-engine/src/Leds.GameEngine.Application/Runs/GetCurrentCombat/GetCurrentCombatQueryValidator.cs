using FluentValidation;

namespace Leds.GameEngine.Application.Runs.GetCurrentCombat;

public sealed class GetCurrentCombatQueryValidator : AbstractValidator<GetCurrentCombatQuery>
{
    public GetCurrentCombatQueryValidator()
    {
        RuleFor(query => query.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}
