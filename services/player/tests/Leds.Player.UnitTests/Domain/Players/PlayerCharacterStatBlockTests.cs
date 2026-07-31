using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerCharacterStatBlockTests
{
    [Fact]
    public void Create_ShouldCreateTheCanonicalStatBlock()
    {
        var stats = PlayerCharacterStatBlock.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 3,
            speed: 10,
            initiative: 9,
            focus: 4,
            mana: 85,
            charge: 2,
            magicAttack: 7,
            magicDefense: 5,
            movement: 6);

        stats.MaxVitality.Should().Be(100);
        stats.AttackPower.Should().Be(12);
        stats.Defense.Should().Be(6);
        stats.StartingGuard.Should().Be(3);
        stats.Speed.Should().Be(10);
        stats.Initiative.Should().Be(9);
        stats.Focus.Should().Be(4);
        stats.Mana.Should().Be(85);
        stats.Charge.Should().Be(2);
        stats.MagicAttack.Should().Be(7);
        stats.MagicDefense.Should().Be(5);
        stats.Movement.Should().Be(6);
    }

    [Fact]
    public void CreateDefaultPorteur_ShouldUseCanonicalDefaults()
    {
        var stats = PlayerCharacterStatBlock.CreateDefaultPorteur();

        stats.MaxVitality.Should().Be(100);
        stats.AttackPower.Should().Be(12);
        stats.Defense.Should().Be(6);
        stats.Speed.Should().Be(10);
        stats.Mana.Should().Be(85);
        stats.MagicAttack.Should().Be(6);
        stats.MagicDefense.Should().Be(3);
        stats.Movement.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldRejectNonPositiveVitality(int value)
    {
        var act = () => Create(maxVitality: value);
        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldRejectNonPositiveSpeed(int value)
    {
        var act = () => Create(speed: value);
        act.Should().Throw<DomainException>().WithMessage("*Speed*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldRejectMovementBelowOne(int value)
    {
        var act = () => Create(movement: value);
        act.Should().Throw<DomainException>().WithMessage("*Movement*");
    }

    [Theory]
    [InlineData(-1, PlayerStatKind.AttackPower)]
    [InlineData(-1, PlayerStatKind.Defense)]
    [InlineData(-1, PlayerStatKind.StartingGuard)]
    [InlineData(-1, PlayerStatKind.Initiative)]
    [InlineData(-1, PlayerStatKind.Focus)]
    [InlineData(-1, PlayerStatKind.Mana)]
    [InlineData(-1, PlayerStatKind.MagicAttack)]
    [InlineData(-1, PlayerStatKind.MagicDefense)]
    public void Create_ShouldRejectNegativeStats(int value, PlayerStatKind kind)
    {
        Action act = kind switch
        {
            PlayerStatKind.AttackPower => () => Create(attackPower: value),
            PlayerStatKind.Defense => () => Create(defense: value),
            PlayerStatKind.StartingGuard => () => Create(startingGuard: value),
            PlayerStatKind.Initiative => () => Create(initiative: value),
            PlayerStatKind.Focus => () => Create(focus: value),
            PlayerStatKind.Mana => () => Create(mana: value),
            PlayerStatKind.MagicAttack => () => Create(magicAttack: value),
            PlayerStatKind.MagicDefense => () => Create(magicDefense: value),
            _ => throw new InvalidOperationException()
        };

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(PlayerStatKind.MaxVitality)]
    [InlineData(PlayerStatKind.AttackPower)]
    [InlineData(PlayerStatKind.Defense)]
    [InlineData(PlayerStatKind.StartingGuard)]
    [InlineData(PlayerStatKind.Speed)]
    [InlineData(PlayerStatKind.Initiative)]
    [InlineData(PlayerStatKind.Focus)]
    [InlineData(PlayerStatKind.Mana)]
    [InlineData(PlayerStatKind.MagicAttack)]
    [InlineData(PlayerStatKind.MagicDefense)]
    public void WithIncrementedStat_ShouldOnlyIncrementTheSelectedStat(PlayerStatKind kind)
    {
        var original = PlayerCharacterStatBlock.CreateDefaultPorteur();
        var incremented = original.WithIncrementedStat(kind);

        var before = Values(original);
        var after = Values(incremented);
        var expectedIncrement = kind switch
        {
            PlayerStatKind.MaxVitality => PlayerCharacterStatBlock.MaxVitalityIncrementPerPoint,
            PlayerStatKind.Mana => PlayerCharacterStatBlock.ManaIncrementPerPoint,
            _ => 1
        };

        after[kind].Should().Be(before[kind] + expectedIncrement);
        foreach (var untouched in before.Keys.Where(candidate => candidate != kind))
            after[untouched].Should().Be(before[untouched]);
    }

    [Fact]
    public void WithIncrementedStat_ShouldRejectUnknownKind()
    {
        var act = () => PlayerCharacterStatBlock.CreateDefaultPorteur()
            .WithIncrementedStat((PlayerStatKind)999);

        act.Should().Throw<DomainException>();
    }

    private static PlayerCharacterStatBlock Create(
        int maxVitality = 100,
        int attackPower = 12,
        int defense = 6,
        int startingGuard = 0,
        int speed = 10,
        int initiative = 10,
        int focus = 0,
        int mana = 0,
        int charge = 0,
        int magicAttack = 0,
        int magicDefense = 0,
        int movement = 4) =>
        PlayerCharacterStatBlock.Create(
            maxVitality,
            attackPower,
            defense,
            startingGuard,
            speed,
            initiative,
            focus,
            mana,
            charge,
            magicAttack,
            magicDefense,
            movement);

    private static Dictionary<PlayerStatKind, int> Values(PlayerCharacterStatBlock stats) =>
        new()
        {
            [PlayerStatKind.MaxVitality] = stats.MaxVitality,
            [PlayerStatKind.AttackPower] = stats.AttackPower,
            [PlayerStatKind.Defense] = stats.Defense,
            [PlayerStatKind.StartingGuard] = stats.StartingGuard,
            [PlayerStatKind.Speed] = stats.Speed,
            [PlayerStatKind.Initiative] = stats.Initiative,
            [PlayerStatKind.Focus] = stats.Focus,
            [PlayerStatKind.Mana] = stats.Mana,
            [PlayerStatKind.MagicAttack] = stats.MagicAttack,
            [PlayerStatKind.MagicDefense] = stats.MagicDefense,
        };
}
