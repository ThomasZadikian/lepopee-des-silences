using MediatR;

namespace Leds.Player.Application.Players.ClearPermanentItemContent;

public sealed record ClearPermanentItemContentCommand(Guid PlayerId, string ItemDefinitionKey)
    : IRequest<PlayerProfileDto>;
