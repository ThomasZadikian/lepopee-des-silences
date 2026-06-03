using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Domain.Nodes;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Application.Events.Resolution;

public sealed class EventContentResolver : IEventContentResolver
{
    private static readonly IReadOnlySet<NodeEventType> UnsupportedStandardPipelineEventTypes =
        new HashSet<NodeEventType>
        {
            NodeEventType.Memory,
            NodeEventType.RoomBoss,
            NodeEventType.FinalBoss
        };

    private readonly IReadOnlyCollection<IEventContentResolutionStrategy> _strategies;

    public EventContentResolver(IEnumerable<IEventContentResolutionStrategy> strategies)
    {
        _strategies = strategies.ToArray();
    }

    public async Task<Result<ResolvedNodeEventContent>> ResolveAsync(
        EventContentResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateContext(context);
        if (validation.IsFailure)
        {
            return Result<ResolvedNodeEventContent>.Failure(validation.Error);
        }

        if (UnsupportedStandardPipelineEventTypes.Contains(context.EventType))
        {
            return Result<ResolvedNodeEventContent>.Failure(Error.Create(
                "event_content.unsupported_standard_pipeline_event_type",
                $"Node event type '{context.EventType}' is not resolved through the standard node event content pipeline."));
        }

        var matchingStrategies = _strategies
            .Where(strategy => strategy.SupportedEventTypes.Contains(context.EventType))
            .ToArray();

        if (matchingStrategies.Length == 0)
        {
            return Result<ResolvedNodeEventContent>.Failure(Error.Create(
                "event_content.strategy_not_found",
                $"No event content resolution strategy was found for node event type '{context.EventType}'."));
        }

        if (matchingStrategies.Length > 1)
        {
            return Result<ResolvedNodeEventContent>.Failure(Error.Create(
                "event_content.ambiguous_strategy",
                $"Multiple event content resolution strategies were found for node event type '{context.EventType}'."));
        }

        return await matchingStrategies[0].ResolveAsync(context, cancellationToken);
    }

    private static Result ValidateContext(EventContentResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Seed))
        {
            return Result.Failure(Error.Create(
                "event_content.seed_required",
                "Seed is required to resolve event content."));
        }

        if (context.RoomDepth < 0)
        {
            return Result.Failure(Error.Create(
                "event_content.room_depth_invalid",
                "Room depth must be non-negative."));
        }

        if (context.NodeDepth < 0)
        {
            return Result.Failure(Error.Create(
                "event_content.node_depth_invalid",
                "Node depth must be non-negative."));
        }

        if (context.EventOrder <= 0)
        {
            return Result.Failure(Error.Create(
                "event_content.event_order_invalid",
                "Event order must be greater than zero."));
        }

        if (context.RiskLevel < 0)
        {
            return Result.Failure(Error.Create(
                "event_content.risk_level_invalid",
                "Risk level must be non-negative."));
        }

        if (string.IsNullOrWhiteSpace(context.RewardProfile))
        {
            return Result.Failure(Error.Create(
                "event_content.reward_profile_required",
                "Reward profile is required."));
        }

        return Result.Success();
    }
}