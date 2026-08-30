using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Domain.PalaceLaws;

/// <summary>
/// Represents the mechanical effect of a <see cref="PalaceLaw"/> on the run.
/// When a law is accepted, each of its effects is applied as a <see cref="RunModifier"/>.
/// </summary>
public sealed record PalaceLawEffect
{
    private PalaceLawEffect(
        RunModifierType modifierType,
        double value,
        RunModifierDuration duration)
    {
        ModifierType = modifierType;
        Value = value;
        Duration = duration;
    }

    public RunModifierType ModifierType { get; }

    /// <summary>
    /// Magnitude of the effect. Interpretation depends on <see cref="ModifierType"/>.
    /// For multiplier types this is the delta (0.15 = +15%).
    /// For additive types this is the raw amount.
    /// </summary>
    public double Value { get; }

    public RunModifierDuration Duration { get; }

    public static PalaceLawEffect Create(
        RunModifierType modifierType,
        double value,
        RunModifierDuration duration)
    {
        if (value == 0)
            throw new DomainException("Palace law effect value must be non-zero.");

        return new PalaceLawEffect(modifierType, value, duration);
    }
}