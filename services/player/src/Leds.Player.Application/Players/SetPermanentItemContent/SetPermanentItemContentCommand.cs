using MediatR;

namespace Leds.Player.Application.Players.SetPermanentItemContent;

public sealed record SetPermanentItemContentCommand(Guid PlayerId, string ItemDefinitionKey, string LiquidDefinitionKey)
    : IRequest<PlayerProfileDto>;
