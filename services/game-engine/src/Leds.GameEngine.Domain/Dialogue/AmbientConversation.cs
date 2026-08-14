using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>
/// An authored ambient/contextual bark — PNJ-to-PNJ or monologue, not a real conversation graph
/// (SFD Système global de dialogues §6). Several <see cref="Lines"/> variants exist so the same
/// trigger doesn't repeat itself verbatim; <see cref="AmbientConversationState"/> is the
/// run-scoped cooldown/rotation tracker, same split as <see cref="Protocol.LocalRule"/>/
/// <see cref="Protocol.LocalRuleState"/>.
/// </summary>
public sealed class AmbientConversation
{
    private readonly List<string> _lines;

    private AmbientConversation(
        string key, AmbientTriggerType triggerType, string triggerKey, List<string> lines, int cooldownSteps)
    {
        Key = key;
        TriggerType = triggerType;
        TriggerKey = triggerKey;
        _lines = lines;
        CooldownSteps = cooldownSteps;
    }

    public string Key { get; }

    public AmbientTriggerType TriggerType { get; }

    /// <summary>The equipment/progression-flag/NPC/room/event key this trigger type checks
    /// against — interpretation is TriggerType-specific and left to the Application layer.</summary>
    public string TriggerKey { get; }

    /// <summary>Text variants — never empty. Rotated by <see cref="AmbientConversationState"/>
    /// so a repeat trigger doesn't always show the same line.</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>Minimum steps between two firings of this same conversation — the
    /// anti-repetition cooldown (§6).</summary>
    public int CooldownSteps { get; }

    public static AmbientConversation Create(
        string key, AmbientTriggerType triggerType, string triggerKey,
        IReadOnlyCollection<string> lines, int cooldownSteps)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Ambient conversation key is required.");
        }

        if (string.IsNullOrWhiteSpace(triggerKey))
        {
            throw new DomainException("Ambient conversation trigger key is required.");
        }

        var lineList = lines?.Where(l => !string.IsNullOrWhiteSpace(l)).ToList() ?? [];
        if (lineList.Count == 0)
        {
            throw new DomainException("Ambient conversation needs at least one line variant.");
        }

        if (cooldownSteps < 0)
        {
            throw new DomainException("Ambient conversation cooldown must be non-negative.");
        }

        return new AmbientConversation(key.Trim(), triggerType, triggerKey.Trim(), lineList, cooldownSteps);
    }
}
