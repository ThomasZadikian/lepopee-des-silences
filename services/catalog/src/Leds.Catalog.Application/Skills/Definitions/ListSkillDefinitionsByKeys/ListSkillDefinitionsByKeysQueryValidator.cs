using FluentValidation;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;

public sealed class ListSkillDefinitionsByKeysQueryValidator
    : AbstractValidator<ListSkillDefinitionsByKeysQuery>
{
    public ListSkillDefinitionsByKeysQueryValidator()
    {
        RuleFor(query => query.Keys)
            .NotNull()
            .WithMessage("Skill definition keys collection is required.")
            .NotEmpty()
            .WithMessage("At least one skill definition key is required.");

        RuleForEach(query => query.Keys)
            .NotEmpty()
            .WithMessage("Each skill definition key must not be empty.")
            .MaximumLength(128)
            .WithMessage("Each skill definition key must not exceed 128 characters.");
    }
}
