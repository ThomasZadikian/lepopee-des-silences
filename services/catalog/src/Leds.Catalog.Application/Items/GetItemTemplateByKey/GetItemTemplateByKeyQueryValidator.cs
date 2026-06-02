using FluentValidation;

namespace Leds.Catalog.Application.Items.GetItemTemplateByKey;

public sealed class GetItemTemplateByKeyQueryValidator
    : AbstractValidator<GetItemTemplateByKeyQuery>
{
    public GetItemTemplateByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Item template key is required.")
            .MaximumLength(128)
            .WithMessage("Item template key must not exceed 128 characters.");
    }
}