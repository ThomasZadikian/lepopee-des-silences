namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// What a <see cref="LocalRule"/> can do once cumulative severity crosses a
/// <see cref="LocalRuleConsequence.SeverityThreshold"/> — SFD Hall d'entrée §V's graduated
/// consequence ladder. Deliberately generic: the Hall's Majordome is the first concrete instance
/// of this vocabulary, not its origin.
/// </summary>
public enum LocalRuleConsequenceType
{
    /// <summary>A present NPC visibly notices — the mildest rung, no state change beyond
    /// what the player sees.</summary>
    Look = 0,

    /// <summary>A present NPC repositions (e.g. steps toward the party, blocks a path).</summary>
    NpcRelocate = 1,

    /// <summary>An explicit spoken/shown warning, distinct from the rule's own
    /// <see cref="LocalRule.WarningMessage"/> shown on the first transgression.</summary>
    Warning = 2,

    /// <summary>A present NPC's disposition shifts — the hook into the dialogue/relationship
    /// system (Ensemble 3), not applied here.</summary>
    AttitudeChange = 3,

    /// <summary>A previously open path/threshold closes.</summary>
    AccessClosure = 4,

    /// <summary>Nearby NPCs' awareness radius/vigilance increases — the hook into
    /// <see cref="Npcs.RoomNpc.RaiseAlert"/>.</summary>
    IncreasedSurveillance = 5,

    /// <summary>A Veilleur is summoned or moves to intercept.</summary>
    VeilleurApproach = 6,

    /// <summary>The transgression escalates into combat — the top rung, never the first
    /// consequence of a rule (Contrat/SFD Hall §V: no automatic combat on a single misstep).</summary>
    Combat = 7,
}
