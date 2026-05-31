using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestRoomFactory
{
    public static Room CreatePlannedRoom(
        int depth = 0,
        RoomType roomType = RoomType.Threshold)
    {
        var layer0A = TestNodeFactory.CreateCombatNode(
            nodeDepth: 0,
            initialState: NodeState.Available);

        var layer0B = TestNodeFactory.CreateMemoryNode(
            nodeDepth: 0,
            initialState: NodeState.Available);

        var layer1A = TestNodeFactory.CreateRestNode(
            nodeDepth: 1,
            parentNodeIds: new[] { layer0A.Id, layer0B.Id });

        var layer1B = TestNodeFactory.CreateItemNode(
            nodeDepth: 1,
            parentNodeIds: new[] { layer0A.Id, layer0B.Id });

        var layer2 = TestNodeFactory.CreateMultiEventNode(
            nodeDepth: 2,
            parentNodeIds: new[] { layer1A.Id, layer1B.Id });

        var boss = TestNodeFactory.CreateRoomBossNode(
            nodeDepth: 3,
            parentNodeIds: new[] { layer2.Id });

        return Room.Create(
            depth,
            roomType,
            ResolveTheme(roomType),
            CreateBossProfile(roomType),
            new[]
            {
                layer0A,
                layer0B,
                layer1A,
                layer1B,
                layer2,
                boss
            });
    }

    public static RoomBossProfile CreateBossProfile(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => RoomBossProfile.Create(
                "threshold-guardian",
                "Gardien du Seuil",
                roomType,
                "High"),

            RoomType.Memory => RoomBossProfile.Create(
                "fractured-archivist",
                "Archiviste Fêlé",
                roomType,
                "High"),

            RoomType.Forest => RoomBossProfile.Create(
                "ash-stag",
                "Cerf de Cendre",
                roomType,
                "High"),

            RoomType.Rupture => RoomBossProfile.Create(
                "broken-fragment",
                "Fragment Brisé",
                roomType,
                "High"),

            RoomType.Silence => RoomBossProfile.Create(
                "mute-watcher",
                "Veilleur Muet",
                roomType,
                "High"),

            RoomType.Antechamber => RoomBossProfile.Create(
                "antechamber-warden",
                "Gardien de l’Antichambre",
                roomType,
                "Very High"),

            RoomType.Final => RoomBossProfile.Create(
                "himlit",
                "Him’Lit",
                roomType,
                "Extreme"),

            _ => RoomBossProfile.Create(
                "unknown-guardian",
                "Gardien Inconnu",
                roomType,
                "High")
        };
    }

    private static string ResolveTheme(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => "Threshold",
            RoomType.Memory => "Memory",
            RoomType.Forest => "Forest",
            RoomType.Rupture => "Rupture",
            RoomType.Silence => "Silence",
            RoomType.Antechamber => "Antechamber",
            RoomType.Final => "Final",
            _ => "Unknown"
        };
    }
}