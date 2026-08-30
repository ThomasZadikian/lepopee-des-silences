using MediatR;

namespace Leds.Player.Application.Internal.ConsumeRunOutcome;

public sealed record ConsumeRunOutcomeCommand(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTime OccurredAtUtc,
    string PayloadJson) : IRequest<ConsumeRunOutcomeResponse>;
