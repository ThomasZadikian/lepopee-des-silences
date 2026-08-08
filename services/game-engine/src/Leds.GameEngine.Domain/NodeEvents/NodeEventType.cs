namespace Leds.GameEngine.Domain.Nodes;

public enum NodeEventType
{
    Combat = 0,
    Elite = 1,
    RoomBoss = 2,
    FinalBoss = 3,
    Item = 4,
    Npc = 5,
    Memory = 6,
    Rest = 7,
    Merchant = 8,
    Law = 9,
    Curse = 10,
    Rare = 11,

    /// <summary>
    /// A room's physical exit toward one specific reachable catalog room (see
    /// MapNode.ExitDestinationRoomKey). Contact behavior is always None — the party must
    /// deliberately stand on it and confirm, never auto-triggered.
    /// </summary>
    Exit = 12
}