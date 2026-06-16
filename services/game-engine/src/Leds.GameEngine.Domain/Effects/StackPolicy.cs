namespace Leds.GameEngine.Domain.Effects;

public enum StackPolicy
{
    None,
    Additive,
    HighestOnly,
    RefreshDuration,
    Replace,
    UniqueBySource,
}
