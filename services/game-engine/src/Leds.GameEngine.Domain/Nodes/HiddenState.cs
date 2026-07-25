namespace Leds.GameEngine.Domain.Nodes;

/// <summary>
/// How discoverable a node is. Exploration is meant to slow the party down and reward looking
/// around, so not every node announces itself: a hidden one has to be found before it can be
/// entered at all.
/// </summary>
public enum HiddenState
{
    /// <summary>Plainly visible once the fog lifts — the ordinary node.</summary>
    None = 0,

    /// <summary>
    /// Something is here, but not what: a slab that rings hollow. Visible as a tell, not as a
    /// node — it cannot be entered until it has been searched.
    /// </summary>
    Hint = 1,

    /// <summary>Searched and opened. Behaves like an ordinary node from here on.</summary>
    Revealed = 2,
}
