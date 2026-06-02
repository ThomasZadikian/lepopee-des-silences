using FluentValidation;

namespace Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;

public sealed class GetPalaceLawDefinitionByKeyQueryValidator
    : AbstractValidator<GetPalaceLawDefinitionByKeyQuery>
{
    public GetPalaceLawDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Palace law definition key is required.")
            .MaximumLength(128)
            .WithMessage("Palace law definition key must not exceed 128 characters.");
    }
}