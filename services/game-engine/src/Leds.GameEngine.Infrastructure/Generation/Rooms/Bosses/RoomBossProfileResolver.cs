using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

public sealed class RoomBossProfileResolver : IRoomBossProfileResolver
{
    public RoomBossProfile Resolve(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold  => RoomBossProfile.Create("threshold-guardian", "Gardien du Seuil",         roomType, "High",     "boss-threshold-guardian-v1"),
            RoomType.Forest     => RoomBossProfile.Create("forest-guardian",    "Gardien des Racines",       roomType, "Medium",   "boss-forest-guardian-v1"),
            RoomType.Rupture    => RoomBossProfile.Create("rupture-warden",     "Fragment de Rupture",       roomType, "High",     "boss-rupture-warden-v1"),
            RoomType.Silence    => RoomBossProfile.Create("silence-warden",     "Voix Éteinte",              roomType, "Unstable", "boss-silence-warden-v1"),
            RoomType.Memory     => RoomBossProfile.Create("memory-keeper",      "Archiviste des Échos",      roomType, "Medium",   "boss-memory-keeper-v1"),
            RoomType.Antechamber => RoomBossProfile.Create("antechamber-warden", "Gardien de l’Antichambre", roomType, "Very High", "boss-antechamber-warden-v1"),
            RoomType.Final      => RoomBossProfile.Create("himlit",             "Him’Lit",                   roomType, "Extreme",  "boss-himlit-v1"),
            _                   => RoomBossProfile.Create("unknown-guardian",   "Gardien Inconnu",           roomType, "High",     "boss-threshold-guardian-v1")
        };
    }
}