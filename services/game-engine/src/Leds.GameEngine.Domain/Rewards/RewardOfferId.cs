namespace Leds.GameEngine.Domain.Rewards;

public readonly record struct RewardOfferId(Guid Value)
{
    public static RewardOfferId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}