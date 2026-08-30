namespace Leds.GameEngine.Domain.Protocol;

/// <summary>
/// What actually happened, for <see cref="LocalRuleEvaluator.Evaluate"/> to check against a
/// <see cref="LocalRule"/>'s condition. Exactly one shape is populated depending on
/// <see cref="LocalRuleConditionType"/> — the evaluator reads only the field matching the rule
/// it's checking.
/// </summary>
public readonly record struct LocalRuleTriggerContext
{
    private LocalRuleTriggerContext(int? partyX, int? partyY, string? interactedTargetKey)
    {
        PartyX = partyX;
        PartyY = partyY;
        InteractedTargetKey = interactedTargetKey;
    }

    public int? PartyX { get; }

    public int? PartyY { get; }

    /// <summary>The object/NPC catalog key just interacted with, for ObjectInteraction/
    /// NpcInteraction conditions.</summary>
    public string? InteractedTargetKey { get; }

    /// <summary>The party occupies (x, y) right now — call once per cell actually entered
    /// (mirrors <see cref="Rooms.Room.MoveParty"/>'s own per-cell stepping), not once per move,
    /// so a zone rule sees a rising edge rather than a level held for several steps.</summary>
    public static LocalRuleTriggerContext ForPosition(int x, int y) => new(x, y, null);

    public static LocalRuleTriggerContext ForInteraction(string targetKey) => new(null, null, targetKey);
}
