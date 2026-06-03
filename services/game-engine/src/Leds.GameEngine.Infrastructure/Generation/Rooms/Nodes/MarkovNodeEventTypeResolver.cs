using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class MarkovNodeEventTypeResolver
{
    private const string Scope = "node-event-type-generation";

    private static readonly IReadOnlySet<NodeEventType> NonPlannableEventTypes =
        new HashSet<NodeEventType>
        {
            NodeEventType.Memory,
            NodeEventType.RoomBoss
        };

    private readonly INodeEventTypeMarkovMatrixProvider _matrixProvider;
    private readonly MarkovTransitionResolver _transitionResolver;

    public MarkovNodeEventTypeResolver(
        INodeEventTypeMarkovMatrixProvider matrixProvider,
        MarkovTransitionResolver transitionResolver)
    {
        _matrixProvider = matrixProvider;
        _transitionResolver = transitionResolver;
    }

    public NodeEventType ResolveNextEventType(NodeEventTypeSelectionContext context)
    {
        ValidateContext(context);

        var matrix = _matrixProvider.GetMatrix(context.MatrixVersion);
        var sourceState = ResolveSourceState(context);

        var sampledState = _transitionResolver.ResolveNextState(
            matrix,
            sourceState,
            context.Seed,
            Scope,
            context.SelectionStep);

        return ResolveWithConstraints(
            matrix,
            sourceState,
            sampledState,
            context.AllowedEventTypes);
    }

    public NodeEventType ResolveNextEventType(
        NodeEventTypeSelectionContext context,
        decimal sample)
    {
        ValidateContext(context);

        var matrix = _matrixProvider.GetMatrix(context.MatrixVersion);
        var sourceState = ResolveSourceState(context);

        var sampledState = _transitionResolver.ResolveNextState(
            matrix,
            sourceState,
            sample);

        return ResolveWithConstraints(
            matrix,
            sourceState,
            sampledState,
            context.AllowedEventTypes);
    }

    private static void ValidateContext(NodeEventTypeSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Seed))
        {
            throw new ArgumentException("Seed is required.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.MatrixVersion))
        {
            throw new ArgumentException("Matrix version is required.", nameof(context));
        }

        if (context.RoomDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                "Room depth must be non-negative.");
        }

        if (context.NodeDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                "Node depth must be non-negative.");
        }

        if (context.EventIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                "Event index must be non-negative.");
        }

        if (context.AllowedEventTypes.Count == 0)
        {
            throw new InvalidOperationException("At least one allowed node event type is required.");
        }
    }

    private static MarkovState ResolveSourceState(NodeEventTypeSelectionContext context)
    {
        if (context.PreviousEventType is not null &&
            !NonPlannableEventTypes.Contains(context.PreviousEventType.Value))
        {
            return MarkovState.Create(context.PreviousEventType.Value.ToString());
        }

        return MarkovState.Create(ResolveRoomTypeAnchor(context.RoomType).ToString());
    }

    private static NodeEventType ResolveRoomTypeAnchor(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Memory => NodeEventType.Npc,
            RoomType.Forest => NodeEventType.Combat,
            RoomType.Rupture => NodeEventType.Curse,
            RoomType.Silence => NodeEventType.Law,
            RoomType.Antechamber => NodeEventType.Rare,
            RoomType.Threshold => NodeEventType.Combat,
            RoomType.Final => NodeEventType.Combat,
            _ => NodeEventType.Combat
        };
    }

    private static NodeEventType ResolveWithConstraints(
        MarkovTransitionMatrix matrix,
        MarkovState sourceState,
        MarkovState sampledState,
        IReadOnlyCollection<NodeEventType> allowedEventTypes)
    {
        var allowed = allowedEventTypes
            .Where(eventType => !NonPlannableEventTypes.Contains(eventType))
            .ToHashSet();

        if (allowed.Count == 0)
        {
            throw new InvalidOperationException(
                "No plannable node event type is allowed by the current constraints.");
        }

        var sampledEventType = ParseEventType(sampledState);
        if (allowed.Contains(sampledEventType))
        {
            return sampledEventType;
        }

        var row = matrix[sourceState];

        var orderedFallbacks = row.Probabilities
            .OrderByDescending(probability => probability.Value)
            .ThenBy(probability => probability.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select(probability => ParseEventType(probability.Key))
            .Where(eventType => !NonPlannableEventTypes.Contains(eventType))
            .Distinct()
            .ToArray();

        foreach (var fallback in orderedFallbacks)
        {
            if (allowed.Contains(fallback))
            {
                return fallback;
            }
        }

        if (allowed.Contains(NodeEventType.Combat))
        {
            return NodeEventType.Combat;
        }

        return allowed
            .OrderBy(eventType => eventType.ToString(), StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static NodeEventType ParseEventType(MarkovState state)
    {
        if (!Enum.TryParse<NodeEventType>(state.Value, ignoreCase: true, out var eventType))
        {
            throw new InvalidOperationException(
                $"Markov state '{state.Value}' cannot be mapped to a node event type.");
        }

        if (NonPlannableEventTypes.Contains(eventType))
        {
            throw new InvalidOperationException(
                $"Node event type '{eventType}' cannot be selected by the node event type Markov matrix.");
        }

        return eventType;
    }
}