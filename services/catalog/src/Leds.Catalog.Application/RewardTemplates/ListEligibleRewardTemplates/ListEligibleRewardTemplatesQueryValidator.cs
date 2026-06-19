using FluentValidation;

namespace Leds.Catalog.Application.RewardTemplates.ListEligibleRewardTemplates;

public sealed class ListEligibleRewardTemplatesQueryValidator : AbstractValidator<ListEligibleRewardTemplatesQuery>
{
    public ListEligibleRewardTemplatesQueryValidator()
    {
        RuleFor(query => query.SourceType)
            .NotEmpty()
            .MaximumLength(80);
    }
}
