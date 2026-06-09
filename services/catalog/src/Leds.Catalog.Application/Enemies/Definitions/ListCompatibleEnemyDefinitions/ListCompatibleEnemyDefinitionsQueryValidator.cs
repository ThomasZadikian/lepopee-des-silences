using FluentValidation;

namespace Leds.Catalog.Application.Enemies.Definitions.ListCompatibleEnemyDefinitions;

public sealed class ListCompatibleEnemyDefinitionsQueryValidator
    : AbstractValidator<ListCompatibleEnemyDefinitionsQuery>
{
    public ListCompatibleEnemyDefinitionsQueryValidator()
    {
        RuleFor(query => query.RoomType)
            .NotEmpty()
            .WithMessage("Room type is required.")
            .MaximumLength(64)
            .WithMessage("Room type must not exceed 64 characters.");

        RuleFor(query => query.RiskLevel)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Risk level must be at least 1.")
            .LessThanOrEqualTo(5)
            .WithMessage("Risk level must not exceed 5.");
    }
}
