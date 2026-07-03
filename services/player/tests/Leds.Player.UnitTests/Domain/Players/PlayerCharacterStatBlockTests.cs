using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterStatBlockTests
{
    [Fact]
    public void Create_ShouldCreateValidStatBlock()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            recovery: 5,
            focus: 0,
            mana: 0,
            charge: 0);

        statBlock.MaxVitality.Should().Be(100);
        statBlock.AttackPower.Should().Be(12);
        statBlock.Defense.Should().Be(6);
        statBlock.StartingGuard.Should().Be(0);
        statBlock.Speed.Should().Be(10);
        statBlock.Initiative.Should().Be(10);
        statBlock.Recovery.Should().Be(5);
        statBlock.Focus.Should().Be(0);
        statBlock.Mana.Should().Be(0);
        statBlock.Charge.Should().Be(0);
    }

    [Fact]
    public void CreateDefaultPorteur_ShouldUseCorrectDefaults()
    {
        var statBlock = PlayerCharacterStatBlock.CreateDefaultPorteur();

        statBlock.MaxVitality.Should().Be(100);
        statBlock.AttackPower.Should().Be(12);
        statBlock.Defense.Should().Be(6);
        statBlock.StartingGuard.Should().Be(0);
        statBlock.Speed.Should().Be(10);
        statBlock.Initiative.Should().Be(10);
        statBlock.Recovery.Should().Be(5);
        statBlock.Focus.Should().Be(0);
        statBlock.Mana.Should().Be(0);
        statBlock.Charge.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectInvalidMaxVitality(int vitality)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            vitality, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Fact]
    public void Create_ShouldAcceptMinimumMaxVitality()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            1, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.MaxVitality.Should().Be(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeAttackPower(int power)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, power, 6, 0, 10, 10, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Attack power*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroAttackPower()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 0, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.AttackPower.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeDefense(int defense)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, defense, 0, 10, 10, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Defense*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroDefense()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 0, 0, 10, 10, 5, 0, 0, 0);

        statBlock.Defense.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeStartingGuard(int guard)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, guard, 10, 10, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Starting guard*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroStartingGuard()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.StartingGuard.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectInvalidSpeed(int speed)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, speed, 10, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Speed*");
    }

    [Fact]
    public void Create_ShouldAcceptMinimumSpeed()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 1, 10, 5, 0, 0, 0);

        statBlock.Speed.Should().Be(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeInitiative(int initiative)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, initiative, 5, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Initiative*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroInitiative()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 0, 5, 0, 0, 0);

        statBlock.Initiative.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeRecovery(int recovery)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, recovery, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Recovery*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroRecovery()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 0, 0, 0, 0);

        statBlock.Recovery.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeFocus(int focus)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, focus, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("*Focus*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroFocus()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.Focus.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeMana(int mana)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, mana, 0);

        act.Should().Throw<DomainException>().WithMessage("*Mana*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroMana()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.Mana.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ShouldRejectNegativeCharge(int charge)
    {
        var act = () => PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, charge);

        act.Should().Throw<DomainException>().WithMessage("*Charge*");
    }

    [Fact]
    public void Create_ShouldAcceptZeroCharge()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.Charge.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldAcceptHighStatValues()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue,
            int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue,
            int.MaxValue, int.MaxValue);

        statBlock.MaxVitality.Should().Be(int.MaxValue);
        statBlock.AttackPower.Should().Be(int.MaxValue);
        statBlock.Defense.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Properties_ShouldBeImmutable()
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            100, 12, 6, 0, 10, 10, 5, 0, 0, 0);

        statBlock.MaxVitality.Should().Be(100);
        statBlock.AttackPower.Should().Be(12);
        statBlock.Defense.Should().Be(6);
        statBlock.StartingGuard.Should().Be(0);
        statBlock.Speed.Should().Be(10);
        statBlock.Initiative.Should().Be(10);
        statBlock.Recovery.Should().Be(5);
        statBlock.Focus.Should().Be(0);
        statBlock.Mana.Should().Be(0);
        statBlock.Charge.Should().Be(0);
    }

    [Theory]
    [InlineData(PlayerStatKind.MaxVitality)]
    [InlineData(PlayerStatKind.AttackPower)]
    [InlineData(PlayerStatKind.Defense)]
    [InlineData(PlayerStatKind.StartingGuard)]
    [InlineData(PlayerStatKind.Speed)]
    [InlineData(PlayerStatKind.Initiative)]
    [InlineData(PlayerStatKind.Recovery)]
    [InlineData(PlayerStatKind.Focus)]
    [InlineData(PlayerStatKind.Mana)]
    [InlineData(PlayerStatKind.Charge)]
    public void WithIncrementedStat_ShouldIncrementOnlyTheSelectedStat(PlayerStatKind kind)
    {
        var original = PlayerCharacterStatBlock.CreateDefaultPorteur();

        var incremented = original.WithIncrementedStat(kind);

        incremented.Should().NotBeSameAs(original);

        var expected = new Dictionary<PlayerStatKind, int>
        {
            [PlayerStatKind.MaxVitality] = original.MaxVitality,
            [PlayerStatKind.AttackPower] = original.AttackPower,
            [PlayerStatKind.Defense] = original.Defense,
            [PlayerStatKind.StartingGuard] = original.StartingGuard,
            [PlayerStatKind.Speed] = original.Speed,
            [PlayerStatKind.Initiative] = original.Initiative,
            [PlayerStatKind.Recovery] = original.Recovery,
            [PlayerStatKind.Focus] = original.Focus,
            [PlayerStatKind.Mana] = original.Mana,
            [PlayerStatKind.Charge] = original.Charge,
        };
        expected[kind] += 1;

        incremented.MaxVitality.Should().Be(expected[PlayerStatKind.MaxVitality]);
        incremented.AttackPower.Should().Be(expected[PlayerStatKind.AttackPower]);
        incremented.Defense.Should().Be(expected[PlayerStatKind.Defense]);
        incremented.StartingGuard.Should().Be(expected[PlayerStatKind.StartingGuard]);
        incremented.Speed.Should().Be(expected[PlayerStatKind.Speed]);
        incremented.Initiative.Should().Be(expected[PlayerStatKind.Initiative]);
        incremented.Recovery.Should().Be(expected[PlayerStatKind.Recovery]);
        incremented.Focus.Should().Be(expected[PlayerStatKind.Focus]);
        incremented.Mana.Should().Be(expected[PlayerStatKind.Mana]);
        incremented.Charge.Should().Be(expected[PlayerStatKind.Charge]);
    }

    [Fact]
    public void WithIncrementedStat_ShouldRejectUnknownKind()
    {
        var statBlock = PlayerCharacterStatBlock.CreateDefaultPorteur();

        var act = () => statBlock.WithIncrementedStat((PlayerStatKind)999);

        act.Should().Throw<DomainException>();
    }
}
