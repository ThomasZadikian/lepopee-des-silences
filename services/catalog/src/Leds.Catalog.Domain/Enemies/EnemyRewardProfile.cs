using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Enemies;

public sealed record EnemyRewardProfile
{
    public int Experience { get; }
    public int Gold { get; }

    private EnemyRewardProfile(int experience, int gold)
    {
        Experience = experience;
        Gold = gold;
    }

    public static EnemyRewardProfile Create(int experience, int gold)
    {
        if (experience < 0)
        {
            throw new DomainException("Enemy experience reward cannot be negative.");
        }

        if (gold < 0)
        {
            throw new DomainException("Enemy gold reward cannot be negative.");
        }

        return new EnemyRewardProfile(experience, gold);
    }
}