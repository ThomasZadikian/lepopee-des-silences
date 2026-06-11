using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Internal.ConsumeRunOutcome;

public sealed class ConsumeRunOutcomeCommandHandler
    : IRequestHandler<ConsumeRunOutcomeCommand, ConsumeRunOutcomeResponse>
{
    private readonly IPlayerProfileRepository _profileRepository;
    private readonly IProcessedIntegrationEventRepository _processedEvents;
    private readonly TimeProvider _timeProvider;

    public ConsumeRunOutcomeCommandHandler(
        IPlayerProfileRepository profileRepository,
        IProcessedIntegrationEventRepository processedEvents,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _processedEvents = processedEvents;
        _timeProvider = timeProvider;
    }

    public async Task<ConsumeRunOutcomeResponse> Handle(
        ConsumeRunOutcomeCommand request,
        CancellationToken cancellationToken)
    {
        if (await _processedEvents.HasProcessedAsync(request.EventId, cancellationToken))
        {
            return new ConsumeRunOutcomeResponse(true, "Event already processed (idempotent).");
        }

        var payload = System.Text.Json.JsonSerializer.Deserialize<RunOutcomePayload>(request.PayloadJson);

        if (payload is null)
        {
            return new ConsumeRunOutcomeResponse(false, "Invalid payload.");
        }

        var playerId = new PlayerId(payload.PlayerId);
        var profile = await _profileRepository.GetByIdAsync(playerId, cancellationToken);

        if (profile is null)
        {
            return new ConsumeRunOutcomeResponse(false, $"Player '{payload.PlayerId}' not found.");
        }

        switch (request.EventType)
        {
            case "RunCompletedIntegrationEvent":
                profile.Progression.IncrementRunsCompleted();
                break;
            case "RunFailedIntegrationEvent":
                profile.Progression.IncrementRunsFailed();
                break;
            case "RunAbandonedIntegrationEvent":
                profile.Progression.IncrementRunsAbandoned();
                break;
            default:
                return new ConsumeRunOutcomeResponse(false, $"Unknown event type: {request.EventType}");
        }

        profile.Touch(_timeProvider.GetUtcNow());
        await _profileRepository.SaveAsync(profile, cancellationToken);
        await _processedEvents.MarkProcessedAsync(request.EventId, request.EventType, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return new ConsumeRunOutcomeResponse(true, null);
    }
}

internal sealed record RunOutcomePayload(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RunId,
    Guid PlayerId,
    string Seed,
    int FinalDepth,
    string GeneratorVersion);
