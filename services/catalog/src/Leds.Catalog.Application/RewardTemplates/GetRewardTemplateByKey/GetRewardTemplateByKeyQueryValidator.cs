using FluentValidation;

namespace Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;

public sealed class GetRewardTemplateByKeyQueryValidator : AbstractValidator<GetRewardTemplateByKeyQuery>
{
    public GetRewardTemplateByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .MaximumLength(160);
    }
}
