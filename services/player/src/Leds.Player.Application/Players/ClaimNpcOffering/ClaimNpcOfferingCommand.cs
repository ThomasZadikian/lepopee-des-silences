using MediatR;

namespace Leds.Player.Application.Players.ClaimNpcOffering;

public sealed record ClaimNpcOfferingCommand(Guid PlayerId, string NpcKey, string OfferingKey, Guid? SourceRunId) : IRequest<PlayerProfileDto>;
