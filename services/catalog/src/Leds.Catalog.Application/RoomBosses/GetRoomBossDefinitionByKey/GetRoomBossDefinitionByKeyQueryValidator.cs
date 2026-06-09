using FluentValidation;

namespace Leds.Catalog.Application.RoomBosses.GetRoomBossDefinitionByKey;

public sealed class GetRoomBossDefinitionByKeyQueryValidator
    : AbstractValidator<GetRoomBossDefinitionByKeyQuery>
{
    public GetRoomBossDefinitionByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .WithMessage("Room boss definition key is required.")
            .MaximumLength(128)
            .WithMessage("Room boss definition key must not exceed 128 characters.");
    }
}
