using FluentValidation;

namespace Leds.Catalog.Application.Enemies.Definitions.GetEnemyDefinitionByKey;

public sealed class GetEnemyDefinitionByKeyQueryValidator
    : AbstractValidator<GetEnemyDefinitionByKeyQuery>
{
    public GetEnemyDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Enemy definition key is required.")
            .MaximumLength(128)
            .WithMessage("Enemy definition key must not exceed 128 characters.");
    }
}
