using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

public sealed class RoomBossProfileResolver : IRoomBossProfileResolver
{
    public RoomBossProfile Resolve(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => RoomBossProfile.Create("threshold-guardian", "Gardien du Seuil", roomType, "High"),
            RoomType.Memory => RoomBossProfile.Create("fractured-archivist", "Archiviste Fêlé", roomType, "High"),
            RoomType.Forest => RoomBossProfile.Create("ash-stag", "Cerf de Cendre", roomType, "High"),
            RoomType.Rupture => RoomBossProfile.Create("broken-fragment", "Fragment Brisé", roomType, "High"),
            RoomType.Silence => RoomBossProfile.Create("mute-watcher", "Veilleur Muet", roomType, "High"),
            RoomType.Antechamber => RoomBossProfile.Create("antechamber-warden", "Gardien de l’Antichambre", roomType, "Very High"),
            RoomType.Final => RoomBossProfile.Create("himlit", "Him’Lit", roomType, "Extreme"),
            _ => RoomBossProfile.Create("unknown-guardian", "Gardien Inconnu", roomType, "High")
        };
    }
}