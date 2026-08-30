namespace Leds.GameEngine.Domain.Nodes;

/// <summary>
/// What happens when the party walks onto a node's cell. Before this existed every node was
/// optional — you stepped on it and chose whether to enter — which meant nothing on the board
/// could actually stand in the party's way.
/// </summary>
public enum ContactBehavior
{
    /// <summary>
    /// Stepping on it opens the side panel; entering stays a deliberate choice. Walking
    /// straight across the cell without entering is allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Avoidable but not ignorable: the party may route around the cell entirely, but setting
    /// foot on it triggers the node immediately, with no prompt. Movement stops there and only
    /// the distance actually walked is charged.
    /// </summary>
    TriggerOnEnter = 1,

    /// <summary>
    /// A lock on the way through: the cell cannot be crossed in transit, only entered. Paths
    /// may end on it, never pass through it — so a node placed on a chokepoint genuinely has
    /// to be dealt with. Triggers on contact like <see cref="TriggerOnEnter"/>.
    /// </summary>
    Blocking = 2,
}
