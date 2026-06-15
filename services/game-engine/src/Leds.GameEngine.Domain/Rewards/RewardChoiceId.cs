namespace Leds.GameEngine.Domain.Rewards;

public readonly record struct RewardChoiceId(Guid Value)
{
    public static RewardChoiceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}