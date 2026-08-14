namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// Secondary relationship dimensions tracked alongside <see cref="NpcRelationship.RelationshipScore"/>
/// and its wound states — SFD Système global de dialogues §4.1: the axes coexist with the
/// existing score/wounds, they do not replace either.
/// </summary>
public enum RelationshipAxis
{
    Trust = 0,
    Respect = 1,
    Proximity = 2,
    Suspicion = 3,
}
