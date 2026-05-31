using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class RoomEventGenerationState : IRoomEventGenerationState
{
    public int EliteCount { get; private set; }

    public int ItemCount { get; private set; }

    public int NpcCount { get; private set; }

    public int RestCount { get; private set; }

    public int MerchantCount { get; private set; }

    public int LawCount { get; private set; }

    public int CurseCount { get; private set; }

    public int RareCount { get; private set; }

    public void Register(NodeEventType eventType)
    {
        switch (eventType)
        {
            case NodeEventType.Elite:
                EliteCount++;
                break;
            case NodeEventType.Item:
                ItemCount++;
                break;
            case NodeEventType.Npc:
                NpcCount++;
                break;
            case NodeEventType.Rest:
                RestCount++;
                break;
            case NodeEventType.Merchant:
                MerchantCount++;
                break;
            case NodeEventType.Law:
                LawCount++;
                break;
            case NodeEventType.Curse:
                CurseCount++;
                break;
            case NodeEventType.Rare:
                RareCount++;
                break;
        }
    }
}