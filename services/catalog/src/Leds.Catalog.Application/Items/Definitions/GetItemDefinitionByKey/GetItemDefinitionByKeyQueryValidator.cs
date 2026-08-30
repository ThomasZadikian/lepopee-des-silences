using FluentValidation;

namespace Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;

public sealed class GetItemDefinitionByKeyQueryValidator : AbstractValidator<GetItemDefinitionByKeyQuery>
{
    public GetItemDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .MaximumLength(160);
    }
}
