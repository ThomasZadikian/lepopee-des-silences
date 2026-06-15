namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class MapNodeParentNodeEntity
{
    public Guid MapNodeId { get; set; }
    public Guid ParentNodeId { get; set; }

    public MapNodeEntity? MapNode { get; set; }
}