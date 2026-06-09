using FluentValidation;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByRoomType;

public sealed class GetRoomBossDefinitionByRoomTypeQueryValidator
    : AbstractValidator<GetRoomBossDefinitionByRoomTypeQuery>
{
    public GetRoomBossDefinitionByRoomTypeQueryValidator()
    {
        RuleFor(query => query.RoomType)
            .NotEmpty()
            .WithMessage("Room type is required.")
            .MaximumLength(64)
            .WithMessage("Room type must not exceed 64 characters.");
    }
}
