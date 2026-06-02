using FluentValidation;

namespace Leds.Catalog.Application.Skills.GetSkillTemplateByKey;

public sealed class GetSkillTemplateByKeyQueryValidator
    : AbstractValidator<GetSkillTemplateByKeyQuery>
{
    public GetSkillTemplateByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Skill template key is required.")
            .MaximumLength(128)
            .WithMessage("Skill template key must not exceed 128 characters.");
    }
}