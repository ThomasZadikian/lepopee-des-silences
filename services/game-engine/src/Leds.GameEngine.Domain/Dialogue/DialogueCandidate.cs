namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>One thing that wants to speak this turn — a dialogue node, an ambient bark, a
/// critical-event line — reduced to just enough for <see cref="DialoguePriorityResolver"/> to
/// arbitrate between several at once.</summary>
public sealed record DialogueCandidate(string Key, DialoguePriority Priority);
