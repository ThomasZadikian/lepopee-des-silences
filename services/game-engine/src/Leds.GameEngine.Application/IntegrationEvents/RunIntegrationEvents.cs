using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.IntegrationEvents;

public sealed record RunCompletedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RunId,
    Guid PlayerId,
    string Seed,
    int FinalDepth,
    string GeneratorVersion);

public sealed record RunFailedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RunId,
    Guid PlayerId,
    string FailureReason,
    int FinalDepth,
    string GeneratorVersion);

public sealed record RunAbandonedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RunId,
    Guid PlayerId,
    int FinalDepth,
    string GeneratorVersion);

public static class RunIntegrationEventFactory
{
    public static RunCompletedIntegrationEvent CreateCompleted(Run run, DateTime occurredAtUtc)
    {
        return new RunCompletedIntegrationEvent(
            Guid.NewGuid(),
            occurredAtUtc,
            run.Id.Value,
            run.PlayerId,
            run.Seed,
            run.CurrentDepth,
            run.GeneratorVersion);
    }

    public static RunFailedIntegrationEvent CreateFailed(Run run, string failureReason, DateTime occurredAtUtc)
    {
        return new RunFailedIntegrationEvent(
            Guid.NewGuid(),
            occurredAtUtc,
            run.Id.Value,
            run.PlayerId,
            failureReason,
            run.CurrentDepth,
            run.GeneratorVersion);
    }

    public static RunAbandonedIntegrationEvent CreateAbandoned(Run run, DateTime occurredAtUtc)
    {
        return new RunAbandonedIntegrationEvent(
            Guid.NewGuid(),
            occurredAtUtc,
            run.Id.Value,
            run.PlayerId,
            run.CurrentDepth,
            run.GeneratorVersion);
    }
}