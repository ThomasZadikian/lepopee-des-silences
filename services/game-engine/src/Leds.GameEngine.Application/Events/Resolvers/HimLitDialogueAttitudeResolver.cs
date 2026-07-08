using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Events.Resolvers;

/// <summary>
/// Him'Lit parle à chaque rencontre (RoomType.Final, tous les BossInterval=10 étages).
/// Son ton dépend de deux signaux : le nombre de fois où on l'a déjà rencontré (son
/// arc, de plus en plus exposé) et l'état de la dernière room Markov traversée avant
/// lui — sa propre room est toujours Neutral par construction
/// (cf. MarkovPalaceRoomStateResolver), donc son humeur immédiate se lit sur la room
/// qui précède, pas sur la sienne.
/// </summary>
public static class HimLitDialogueAttitudeResolver
{
    public static string ResolveNodeKey(int timesMet, PalaceRoomState precedingRoomState)
    {
        var palier = timesMet switch
        {
            <= 1 => 1,
            <= 4 => 2,
            _ => 3
        };

        var isFracture = precedingRoomState is PalaceRoomState.Painful
            or PalaceRoomState.Enraged
            or PalaceRoomState.Violent;

        return $"rencontre-p{palier}-{(isFracture ? "fracture" : "calme")}";
    }
}
