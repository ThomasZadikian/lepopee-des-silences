namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// What a <see cref="LocalRule"/> watches for — SFD Hall d'entrée §V's "condition" step. Generic
/// across any room, not a Hall-specific vocabulary: a threshold crossing is just a one-cell zone,
/// so it is not a separate case here.
/// </summary>
public enum LocalRuleConditionType
{
    /// <summary>The party occupies one of <see cref="LocalRule.ConditionCells"/> — covers both a
    /// bounded zone (e.g. the carpet) and a single-cell threshold alike.</summary>
    ZoneEntry = 0,

    /// <summary>The party interacts with a specific catalog-keyed object/node
    /// (<see cref="LocalRule.ConditionTargetKey"/>).</summary>
    ObjectInteraction = 1,

    /// <summary>The party interacts with a specific catalog-keyed NPC
    /// (<see cref="LocalRule.ConditionTargetKey"/>, joined against <see cref="Npcs.RoomNpc.CatalogNpcKey"/>).</summary>
    NpcInteraction = 2,
}
