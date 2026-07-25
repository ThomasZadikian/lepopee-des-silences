namespace Leds.GameEngine.Domain.Nodes;

/// <summary>
/// The visible warning a node gives off before the party steps on it. Only meaningful for a
/// node that triggers on contact (see <see cref="ContactBehavior"/>): it is what lets a player
/// read the ground and route around a threat instead of being ambushed by it.
/// </summary>
public enum DangerTell
{
    /// <summary>
    /// No warning at all — the ambush. Deliberately indistinguishable from plain floor: this
    /// is the intended design, not a gap to fill in with a subtle marker later.
    /// </summary>
    None = 0,

    /// <summary>Claw marks and bones.</summary>
    Tracks = 1,

    /// <summary>An uneasy halo.</summary>
    Glow = 2,

    /// <summary>Stains and dead growth.</summary>
    Blight = 3,
}
