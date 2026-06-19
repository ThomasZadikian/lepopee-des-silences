using FluentValidation;

namespace Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;

public sealed class GetCurseDefinitionByKeyQueryValidator : AbstractValidator<GetCurseDefinitionByKeyQuery>
{
    public GetCurseDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .MaximumLength(160);
    }
}
