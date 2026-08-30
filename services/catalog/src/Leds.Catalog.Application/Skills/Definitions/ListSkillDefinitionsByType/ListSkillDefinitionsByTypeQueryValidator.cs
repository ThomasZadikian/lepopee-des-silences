using FluentValidation;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;

public sealed class ListSkillDefinitionsByTypeQueryValidator
    : AbstractValidator<ListSkillDefinitionsByTypeQuery>
{
    public ListSkillDefinitionsByTypeQueryValidator()
    {
        RuleFor(query => query.SkillType)
            .NotEmpty()
            .WithMessage("Skill type is required.")
            .MaximumLength(64)
            .WithMessage("Skill type must not exceed 64 characters.");
    }
}
