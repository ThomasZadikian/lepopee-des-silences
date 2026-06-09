using FluentValidation;

namespace Leds.Catalog.Application.Enemies.Definitions.ListEnemyDefinitionsByRoomType;

public sealed class ListEnemyDefinitionsByRoomTypeQueryValidator
    : AbstractValidator<ListEnemyDefinitionsByRoomTypeQuery>
{
    public ListEnemyDefinitionsByRoomTypeQueryValidator()
    {
        RuleFor(query => query.RoomType)
            .NotEmpty()
            .WithMessage("Room type is required.")
            .MaximumLength(64)
            .WithMessage("Room type must not exceed 64 characters.");
    }
}
