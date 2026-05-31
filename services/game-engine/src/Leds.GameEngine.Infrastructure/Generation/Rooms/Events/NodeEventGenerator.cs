using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Common;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class NodeEventGenerator : INodeEventGenerator
{
    private readonly INodeEventCandidateResolver _candidateResolver;

    public NodeEventGenerator(INodeEventCandidateResolver candidateResolver)
    {
        _candidateResolver = candidateResolver;
    }

    public IReadOnlyCollection<NodeEvent> Generate(
        Random random,
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState state)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(state);

        var firstEventType = ResolveEventType(
            random,
            roomType,
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
                random,
                roomType,
                totalNodeCount,
                state,
                events);

            events.Add(NodeEvent.Create(eventType, order));
            state.Register(eventType);
        }

        return events;
    }

    private NodeEventType ResolveEventType(
        Random random,
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState state,
        IReadOnlyCollection<NodeEvent> currentNodeEvents)
    {
        var candidates = _candidateResolver.ResolveCandidates(
            roomType,
            totalNodeCount,
            state,
            currentNodeEvents);

        return candidates[random.Next(candidates.Count)];
    }
}