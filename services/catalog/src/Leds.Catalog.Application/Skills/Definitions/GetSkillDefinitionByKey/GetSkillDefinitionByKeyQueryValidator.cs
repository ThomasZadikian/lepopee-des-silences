using FluentValidation;

namespace Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;

public sealed class GetSkillDefinitionByKeyQueryValidator
    : AbstractValidator<GetSkillDefinitionByKeyQuery>
{
    public GetSkillDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Skill definition key is required.")
            .MaximumLength(128)
            .WithMessage("Skill definition key must not exceed 128 characters.");
    }
}
