using FluentValidation;

namespace Leds.GameEngine.Application.Runs.GetRunById;

public sealed class GetRunByIdQueryValidator : AbstractValidator<GetRunByIdQuery>
{
    public GetRunByIdQueryValidator()
    {
        RuleFor(query => query.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");
    }
}