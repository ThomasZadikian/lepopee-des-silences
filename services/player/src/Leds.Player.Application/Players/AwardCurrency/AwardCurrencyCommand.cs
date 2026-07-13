using MediatR;

namespace Leds.Player.Application.Players.AwardCurrency;

public sealed record AwardCurrencyCommand(Guid PlayerId, int Amount) : IRequest<PlayerProfileDto>;
