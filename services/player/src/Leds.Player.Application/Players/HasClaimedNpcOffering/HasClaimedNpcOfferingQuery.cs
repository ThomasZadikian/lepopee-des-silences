using MediatR;

namespace Leds.Player.Application.Players.HasClaimedNpcOffering;

public sealed record HasClaimedNpcOfferingQuery(Guid PlayerId, string NpcKey, string OfferingKey) : IRequest<bool>;
