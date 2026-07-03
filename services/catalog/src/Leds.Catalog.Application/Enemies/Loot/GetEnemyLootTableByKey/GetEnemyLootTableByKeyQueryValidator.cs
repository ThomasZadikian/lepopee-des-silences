using FluentValidation;

namespace Leds.Catalog.Application.Enemies.Loot.GetEnemyLootTableByKey;

public sealed class GetEnemyLootTableByKeyQueryValidator
    : AbstractValidator<GetEnemyLootTableByKeyQuery>
{
    public GetEnemyLootTableByKeyQueryValidator()
    {
        RuleFor(query => query.EnemyKey)
            .NotEmpty()
            .WithMessage("Enemy key is required.")
            .MaximumLength(160)
            .WithMessage("Enemy key must not exceed 160 characters.");
    }
}
