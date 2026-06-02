using FluentValidation;

namespace Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;

public sealed class GetEventTemplateByKeyQueryValidator
    : AbstractValidator<GetEventTemplateByKeyQuery>
{
    public GetEventTemplateByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Event template key is required.")
            .MaximumLength(128)
            .WithMessage("Event template key must not exceed 128 characters.");
    }
}