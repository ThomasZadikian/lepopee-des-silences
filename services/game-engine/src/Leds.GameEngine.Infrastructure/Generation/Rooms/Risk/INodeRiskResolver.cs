using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Risk;

public interface INodeRiskResolver
{
    int Resolve(IReadOnlyCollection<NodeEvent> events, Random random);
}