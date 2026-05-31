using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public interface IRoomEventGenerationState
{
    int EliteCount { get; }

    int ItemCount { get; }

    int NpcCount { get; }

    int RestCount { get; }

    int MerchantCount { get; }

    int LawCount { get; }

    int CurseCount { get; }

    int RareCount { get; }

    void Register(NodeEventType eventType);
}