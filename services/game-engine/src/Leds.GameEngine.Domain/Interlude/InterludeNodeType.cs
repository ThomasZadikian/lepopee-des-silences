namespace Leds.GameEngine.Domain.Interlude;

/// <summary>
/// The category of an interlude hub node.
/// Each type maps to a distinct panel or interaction zone in the interlude UI.
/// </summary>
public enum InterludeNodeType
{
    /// <summary>Player character sheet / stats panel.</summary>
    Player = 0,

    /// <summary>Elise NPC — narrative companion, future dialogue actions.</summary>
    Elise = 1,

    /// <summary>Inventory / backpack panel.</summary>
    Inventory = 2,

    /// <summary>Run journal — explored rooms, events, laws.</summary>
    Journal = 3,

    /// <summary>Placeholder slot — reserved for future interlude features.</summary>
    Placeholder = 4,
}