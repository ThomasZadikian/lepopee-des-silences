using Leds.GameEngine.Infrastructure.Generation.Common;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Layers;

public sealed class RoomNodeLayerPlanner : IRoomNodeLayerPlanner
{
    public int ResolveNormalLayerCount(Random random, int normalNodeCount)
    {
        ArgumentNullException.ThrowIfNull(random);

        var maxLayerCount = Math.Min(4, normalNodeCount);
        var minLayerCount = normalNodeCount <= 5 ? 2 : 3;

        if (minLayerCount > maxLayerCount)
        {
            minLayerCount = maxLayerCount;
        }

        return random.Next(minLayerCount, maxLayerCount + 1);
    }

    public int ResolveNodeCountForLayer(
        Random random,
        int nodeDepth,
        int normalLayerCount,
        int remainingNormalNodes)
    {
        ArgumentNullException.ThrowIfNull(random);

        var remainingLayersAfterThisOne = normalLayerCount - nodeDepth - 1;

        var minimumNodeCountForLayer = nodeDepth == 0
            ? RoomGenerationConstants.MinInitialLayerNodeCount
            : 1;

        var minForThisLayer = Math.Max(
            minimumNodeCountForLayer,
            remainingNormalNodes - remainingLayersAfterThisOne * RoomGenerationConstants.MaxLayerNodeCount);

        var maxForThisLayer = Math.Min(
            RoomGenerationConstants.MaxLayerNodeCount,
            remainingNormalNodes - remainingLayersAfterThisOne);

        if (minForThisLayer > maxForThisLayer)
        {
            throw new InvalidOperationException(
                "Room plan generation failed to compute a valid node count for the current layer.");
        }

        return random.Next(minForThisLayer, maxForThisLayer + 1);
    }
}