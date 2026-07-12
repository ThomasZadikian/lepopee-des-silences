using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class NpcReputationScore
{
    private NpcReputationScore(string npcKey, int score, int timesMet, string? currentDialogueNodeKey, DateTimeOffset updatedAtUtc)
    {
        NpcKey = npcKey;
        Score = score;
        TimesMet = timesMet;
        CurrentDialogueNodeKey = currentDialogueNodeKey;
        UpdatedAtUtc = updatedAtUtc;
    }

    public string NpcKey { get; }
    public int Score { get; private set; }
    public int TimesMet { get; private set; }
    public string? CurrentDialogueNodeKey { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ApplyDelta(int delta, int timesMetDelta, string? dialogueNodeKey, DateTimeOffset now)
    {
        Score += delta;
        TimesMet += timesMetDelta;
        if (dialogueNodeKey is not null)
            CurrentDialogueNodeKey = dialogueNodeKey;
        UpdatedAtUtc = now;
    }

    public static NpcReputationScore Create(string npcKey, int score, int timesMet, string? currentDialogueNodeKey, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(npcKey))
            throw new DomainException("NPC key is required.");

        return new NpcReputationScore(npcKey.Trim(), score, timesMet, currentDialogueNodeKey, now);
    }

    public static NpcReputationScore Rehydrate(string npcKey, int score, int timesMet, string? currentDialogueNodeKey, DateTimeOffset updatedAtUtc)
    {
        return new NpcReputationScore(npcKey, score, timesMet, currentDialogueNodeKey, updatedAtUtc);
    }
}
