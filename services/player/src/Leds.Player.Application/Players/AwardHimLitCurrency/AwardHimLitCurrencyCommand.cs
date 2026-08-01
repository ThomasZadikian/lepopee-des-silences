using MediatR;

namespace Leds.Player.Application.Players.AwardHimLitCurrency;

public sealed record AwardHimLitCurrencyCommand(Guid PlayerId, int Amount) : IRequest<PlayerProfileDto>;
