using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class EnemyStatBlockTests
{
    [Fact]
    public void Create_ShouldAcceptBoundaryValues()
    {
        var block = EnemyStatBlock.Create(1, 0, 0, 0, 1, 0, 0);

        block.MaxVitality.Should().Be(1);
        block.AttackPower.Should().Be(0);
        block.Speed.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 1, 1, 1, 1, 1, 1, "max vitality")]
    [InlineData(1, -1, 1, 1, 1, 1, 1, "attack power")]
    [InlineData(1, 1, -1, 1, 1, 1, 1, "defense")]
    [InlineData(1, 1, 1, -1, 1, 1, 1, "starting guard")]
    [InlineData(1, 1, 1, 1, 0, 1, 1, "speed")]
    [InlineData(1, 1, 1, 1, 1, -1, 1, "initiative")]
    [InlineData(1, 1, 1, 1, 1, 1, -1, "focus")]
    public void Create_ShouldRejectInvalidStat(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int focus,
        string expectedMessage)
    {
        var act = () => EnemyStatBlock.Create(
            maxVitality, attackPower, defense, startingGuard, speed, initiative, focus);

        act.Should().Throw<DomainException>()
            .WithMessage($"*{expectedMessage}*");
    }
}
