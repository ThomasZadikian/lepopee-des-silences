namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// How much a <see cref="RoomNpc"/> currently perceives the party — Contrat canonique §38 /
/// SFD Hall d'entrée §IV ("un PNJ peut être présent, actif et en interaction avec son
/// environnement sans avoir encore conscience du groupe"). Proximity, line of sight, a direct
/// interaction, an authored event, or a transgression can move a NPC up or down this scale;
/// nothing else does.
/// </summary>
public enum NpcAwarenessState
{
    /// <summary>Present and acting on its own, but has not registered the party at all.</summary>
    Unaware = 0,

    /// <summary>Has noticed the party — may react, approach, or comment, but is not hostile.</summary>
    Aware = 1,

    /// <summary>Actively watching or reacting to a transgression — the step before hostility.</summary>
    Alert = 2,
}
