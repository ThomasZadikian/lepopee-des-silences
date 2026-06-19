using FluentValidation;

namespace Leds.Catalog.Application.EffectSets.GetEffectSetByKey;

public sealed class GetEffectSetByKeyQueryValidator : AbstractValidator<GetEffectSetByKeyQuery>
{
    public GetEffectSetByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .MaximumLength(160);
    }
}
