using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Common;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class NodeEventGenerator : INodeEventGenerator
{
    private readonly INodeEventCandidateResolver _candidateResolver;
    private readonly MarkovNodeEventTypeResolver _markovNodeEventTypeResolver;

    public NodeEventGenerator(
        INodeEventCandidateResolver candidateResolver,
        MarkovNodeEventTypeResolver markovNodeEventTypeResolver)
    {
        _candidateResolver = candidateResolver;
        _markovNodeEventTypeResolver = markovNodeEventTypeResolver;
    }

    public IReadOnlyCollection<NodeEvent> Generate(
        Random random,
        string seed,
        string matrixVersion,
        RoomType roomType,
        int roomDepth,
        int nodeDepth,
        int totalNodeCount,
        IRoomEventGenerationState state)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        ArgumentException.ThrowIfNullOrWhiteSpace(matrixVersion);
        ArgumentNullException.ThrowIfNull(state);

        var firstEventType = ResolveEventType(
            seed,
            matrixVersion,
            roomType,
            roomDepth,
            nodeDepth,
            eventIndex: 0,
            totalNodeCount,
            state,
            Array.Empty<NodeEvent>());

        var events = new List<NodeEvent>
        {
            NodeEvent.Create(firstEventType, order: 1)
        };

        state.Register(firstEventType);

        if (firstEventType == NodeEventType.Rest)
        {
            return events;
        }

        var eventCount = random.Next(
            RoomGenerationConstants.MinNodeEventCount,
            RoomGenerationConstants.MaxNodeEventCount + 1);

        for (var order = 2; order <= eventCount; order++)
        {
            var eventType = ResolveEventType(
                seed,
                matrixVersion,
                roomType,
                roomDepth,
                nodeDepth,
                eventIndex: order - 1,
                totalNodeCount,
                state,
                events);

            events.Add(NodeEvent.Create(eventType, order));
            state.Register(eventType);
        }

        return events;
    }

    private NodeEventType ResolveEventType(
        string seed,
        string matrixVersion,
        RoomType roomType,
        int roomDepth,
        int nodeDepth,
        int eventIndex,
        int totalNodeCount,
        IRoomEventGenerationState state,
        IReadOnlyCollection<NodeEvent> currentNodeEvents)
    {
        var candidates = _candidateResolver.ResolveCandidates(
            roomType,
            totalNodeCount,
            state,
            currentNodeEvents);

        var previousEventType = currentNodeEvents
            .OrderByDescending(nodeEvent => nodeEvent.Order)
            .Select(nodeEvent => (NodeEventType?)nodeEvent.EventType)
            .FirstOrDefault();

        return _markovNodeEventTypeResolver.ResolveNextEventType(
            new NodeEventTypeSelectionContext(
                Seed: seed,
                MatrixVersion: matrixVersion,
                RoomType: roomType,
                RoomDepth: roomDepth,
                NodeDepth: nodeDepth,
                EventIndex: eventIndex,
                PreviousEventType: previousEventType,
                AllowedEventTypes: candidates));
    }
}