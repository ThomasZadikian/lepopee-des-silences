using FluentValidation;

namespace Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;

public sealed class GetEnemyTemplateByKeyQueryValidator
    : AbstractValidator<GetEnemyTemplateByKeyQuery>
{
    public GetEnemyTemplateByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Enemy template key is required.")
            .MaximumLength(128)
            .WithMessage("Enemy template key must not exceed 128 characters.");
    }
}