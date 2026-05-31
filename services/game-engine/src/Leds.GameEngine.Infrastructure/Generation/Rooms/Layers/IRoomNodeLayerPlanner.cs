namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Layers;

public interface IRoomNodeLayerPlanner
{
    int ResolveNormalLayerCount(Random random, int normalNodeCount);

    int ResolveNodeCountForLayer(
        Random random,
        int nodeDepth,
        int normalLayerCount,
        int remainingNormalNodes);
}