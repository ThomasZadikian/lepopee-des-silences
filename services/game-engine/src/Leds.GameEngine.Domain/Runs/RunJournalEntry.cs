namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// One literary line of the Carnet de bord, tagged with the room it happened in so the
/// frontend can lay the journal out as one page per room.
/// </summary>
public sealed record RunJournalEntry(int RoomIndex, string? RoomDisplayName, string Text);
