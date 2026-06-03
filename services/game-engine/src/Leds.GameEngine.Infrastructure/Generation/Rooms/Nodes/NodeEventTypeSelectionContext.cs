using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed record NodeEventTypeSelectionContext(
    string Seed,
    string MatrixVersion,
    RoomType RoomType,
    int RoomDepth,
    int NodeDepth,
    int EventIndex,
    NodeEventType? PreviousEventType,
    IReadOnlyCollection<NodeEventType> AllowedEventTypes)
{
    public int SelectionStep
    {
        get
        {
            checked
            {
                return (RoomDepth * 10_000) + (NodeDepth * 100) + EventIndex;
            }
        }
    }
}