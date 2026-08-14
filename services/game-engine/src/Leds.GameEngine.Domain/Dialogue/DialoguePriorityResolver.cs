namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>
/// Picks the single candidate that gets to speak — SFD Système global de dialogues §9.1-9.2.
/// A pure function over a fixed input list IS the "coherent snapshot" §9.2 asks for: nothing
/// about the candidate set changes mid-resolution, so calling this once and acting on its result
/// is by construction consistent.
/// </summary>
public static class DialoguePriorityResolver
{
    /// <summary>Null when given no candidates. Ties keep the winner from earliest in
    /// <paramref name="candidates"/> — <see cref="Enumerable.OrderBy"/> is a stable sort, so this
    /// falls out of the ordering rather than needing an explicit tiebreaker.</summary>
    public static DialogueCandidate? SelectHighestPriority(IReadOnlyCollection<DialogueCandidate> candidates)
    {
        return candidates.Count == 0
            ? null
            : candidates.OrderBy(c => (int)c.Priority).First();
    }
}
