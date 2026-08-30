using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>Run-scoped cooldown/rotation progress against one <see cref="AmbientConversation"/>.</summary>
public sealed class AmbientConversationState
{
    private AmbientConversationState(string ambientConversationKey, int? lastTriggeredStep, int timesTriggered)
    {
        AmbientConversationKey = ambientConversationKey;
        LastTriggeredStep = lastTriggeredStep;
        TimesTriggered = timesTriggered;
    }

    public string AmbientConversationKey { get; }

    public int? LastTriggeredStep { get; private set; }

    public int TimesTriggered { get; private set; }

    public static AmbientConversationState Create(string ambientConversationKey)
    {
        if (string.IsNullOrWhiteSpace(ambientConversationKey))
        {
            throw new DomainException("Ambient conversation state requires a key.");
        }

        return new AmbientConversationState(ambientConversationKey.Trim(), lastTriggeredStep: null, timesTriggered: 0);
    }

    public static AmbientConversationState Rehydrate(
        string ambientConversationKey, int? lastTriggeredStep, int timesTriggered) =>
        new(ambientConversationKey, lastTriggeredStep, timesTriggered);

    public bool CanTrigger(int currentStep, int cooldownSteps) =>
        LastTriggeredStep is not { } lastStep || currentStep - lastStep >= cooldownSteps;

    /// <summary>Records a firing at <paramref name="currentStep"/> and returns which
    /// <see cref="AmbientConversation.Lines"/> index to show — round-robin over
    /// <paramref name="lineCount"/>, so consecutive firings cycle variants instead of always
    /// repeating the first one.</summary>
    public int RegisterTrigger(int currentStep, int lineCount)
    {
        if (lineCount < 1)
        {
            throw new DomainException("Ambient conversation line count must be at least 1.");
        }

        var variantIndex = TimesTriggered % lineCount;
        LastTriggeredStep = currentStep;
        TimesTriggered++;
        return variantIndex;
    }
}
