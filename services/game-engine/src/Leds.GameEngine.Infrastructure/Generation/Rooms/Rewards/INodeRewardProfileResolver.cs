using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Rewards;

public interface INodeRewardProfileResolver
{
    string Resolve(IReadOnlyCollection<NodeEvent> events);
}