namespace Leds.GameEngine.Domain.Effects;

public sealed record RuntimeEffect(
    EffectType EffectType,
    EffectTargetScope TargetScope,
    decimal Value,
    ValueMode ValueMode,
    EffectDuration Duration,
    StackPolicy StackPolicy,
    string? Condition,
    int Order,
    string? BehaviorTag,
    string? GenerationTag,
    string? SelectionGroup)
{
    public bool IsImmediate => Duration == EffectDuration.Immediate;

    public bool IsPersistent => Duration != EffectDuration.Immediate;

    public decimal ResolveValue(decimal baseValue)
    {
        return ValueMode switch
        {
            Domain.Effects.ValueMode.Flat => Value,
            Domain.Effects.ValueMode.Percent => baseValue * Value / 100m,
            Domain.Effects.ValueMode.Multiplier => baseValue * Value,
            _ => Value,
        };
    }
}
