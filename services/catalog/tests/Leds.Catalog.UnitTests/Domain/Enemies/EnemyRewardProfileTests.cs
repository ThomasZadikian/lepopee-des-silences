using FluentAssertions;
using Leds.Catalog.Domain.Enemies;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.UnitTests.Domain.Enemies;

public sealed class EnemyRewardProfileTests
{
    [Fact]
    public void Create_ShouldCreateProfile_WhenExperienceAndGoldAreValid()
    {
        var profile = EnemyRewardProfile.Create(experience: 100, gold: 50);

        profile.Experience.Should().Be(100);
        profile.Gold.Should().Be(50);
    }

    [Fact]
    public void Create_ShouldCreateProfile_WhenExperienceAndGoldAreZero()
    {
        var profile = EnemyRewardProfile.Create(experience: 0, gold: 0);

        profile.Experience.Should().Be(0);
        profile.Gold.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldCreateProfile_WhenExperienceAndGoldAreLargeValues()
    {
        var profile = EnemyRewardProfile.Create(experience: int.MaxValue, gold: int.MaxValue);

        profile.Experience.Should().Be(int.MaxValue);
        profile.Gold.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenExperienceIsNegative()
    {
        var act = () => EnemyRewardProfile.Create(experience: -1, gold: 0);

        act.Should().Throw<DomainException>()
            .WithMessage("Enemy experience reward cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenGoldIsNegative()
    {
        var act = () => EnemyRewardProfile.Create(experience: 0, gold: -1);

        act.Should().Throw<DomainException>()
            .WithMessage("Enemy gold reward cannot be negative.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenBothAreNegative()
    {
        var act = () => EnemyRewardProfile.Create(experience: -10, gold: -5);

        act.Should().Throw<DomainException>()
            .WithMessage("Enemy experience reward cannot be negative.");
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenExperienceAndGoldMatch()
    {
        var profile1 = EnemyRewardProfile.Create(100, 50);
        var profile2 = EnemyRewardProfile.Create(100, 50);

        profile1.Should().Be(profile2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenExperienceDiffers()
    {
        var profile1 = EnemyRewardProfile.Create(100, 50);
        var profile2 = EnemyRewardProfile.Create(200, 50);

        profile1.Should().NotBe(profile2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenGoldDiffers()
    {
        var profile1 = EnemyRewardProfile.Create(100, 50);
        var profile2 = EnemyRewardProfile.Create(100, 100);

        profile1.Should().NotBe(profile2);
    }
}
