using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Local exception layered over the immutable run matrix. Equipment uses a null
/// duration; statuses may provide a finite number of holder activations.
/// </summary>
public sealed record EmotionalAffinityModifier
{
    private EmotionalAffinityModifier(
        string sourceKey,
        EmotionalType incomingRegister,
        DamageEffectiveness? outcomeOverride,
        int multiplierPercent,
        int priority,
        int? remainingActivations)
    {
        SourceKey = sourceKey;
        IncomingRegister = incomingRegister;
        OutcomeOverride = outcomeOverride;
        MultiplierPercent = multiplierPercent;
        Priority = priority;
        RemainingActivations = remainingActivations;
    }

    public string SourceKey { get; }
    public EmotionalType IncomingRegister { get; }
    public DamageEffectiveness? OutcomeOverride { get; }
    public int MultiplierPercent { get; }
    public int Priority { get; }
    public int? RemainingActivations { get; private set; }
    public bool IsExpired => RemainingActivations == 0;

    public static EmotionalAffinityModifier Create(
        string sourceKey,
        EmotionalType incomingRegister,
        DamageEffectiveness? outcomeOverride = null,
        int multiplierPercent = 0,
        int priority = 0,
        int? durationActivations = null)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new DomainException("Affinity modifier source key is required.");
        if (outcomeOverride is null && multiplierPercent == 0)
            throw new DomainException("Affinity modifier must override an outcome or a multiplier.");
        if (durationActivations is <= 0)
            throw new DomainException("Affinity modifier duration must be positive when supplied.");

        return new EmotionalAffinityModifier(
            sourceKey.Trim(), incomingRegister, outcomeOverride, multiplierPercent,
            priority, durationActivations);
    }

    public static EmotionalAffinityModifier Rehydrate(
        string sourceKey,
        EmotionalType incomingRegister,
        DamageEffectiveness? outcomeOverride,
        int multiplierPercent,
        int priority,
        int? remainingActivations) =>
        Create(sourceKey, incomingRegister, outcomeOverride, multiplierPercent, priority, remainingActivations);

    public void ConsumeActivation()
    {
        if (RemainingActivations is > 0)
            RemainingActivations--;
    }
}
