namespace Leds.GameEngine.Domain.Interlude;

/// <summary>
/// Identifies the action triggered when a player interacts with an interlude node.
/// </summary>
public enum InterludeActionKey
{
    /// <summary>Open the player character sheet.</summary>
    ViewPlayer = 0,

    /// <summary>Initiate dialogue with Elise.</summary>
    TalkToElise = 1,

    /// <summary>Open the inventory / backpack.</summary>
    OpenInventory = 2,

    /// <summary>Open the run journal.</summary>
    OpenJournal = 3,

    /// <summary>Reserved placeholder — no action yet.</summary>
    Placeholder = 4,
}