using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCharacterStatSnapshotTests
{
    [Fact]
    public void Create_ShouldSetAllStats_IncludingMagicAttackAndMagicDefense()
    {
        var snapshot = RunCharacterStatSnapshot.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0,
            magicAttack: 9,
            magicDefense: 4);

        snapshot.MagicAttack.Should().Be(9);
        snapshot.MagicDefense.Should().Be(4);
    }

    [Fact]
    public void Create_ShouldDefaultMagicAttackAndMagicDefense_ToZero_WhenNotProvided()
    {
        var snapshot = RunCharacterStatSnapshot.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0);

        snapshot.MagicAttack.Should().Be(0);
        snapshot.MagicDefense.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMagicAttackIsNegative()
    {
        var act = () => RunCharacterStatSnapshot.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0,
            magicAttack: -1);

        act.Should().Throw<DomainException>().WithMessage("*Magic attack*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMagicDefenseIsNegative()
    {
        var act = () => RunCharacterStatSnapshot.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0,
            magicDefense: -1);

        act.Should().Throw<DomainException>().WithMessage("*Magic defense*");
    }

    [Fact]
    public void CreateDefault_ShouldSetMagicAttackAndMagicDefense_ToZero()
    {
        var snapshot = RunCharacterStatSnapshot.CreateDefault();

        snapshot.MagicAttack.Should().Be(0);
        snapshot.MagicDefense.Should().Be(0);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreMagicAttackAndMagicDefense()
    {
        var id = Guid.NewGuid();

        var snapshot = RunCharacterStatSnapshot.Rehydrate(
            id,
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: 0,
            charge: 0,
            magicAttack: 9,
            magicDefense: 4);

        snapshot.Id.Should().Be(id);
        snapshot.MagicAttack.Should().Be(9);
        snapshot.MagicDefense.Should().Be(4);
    }
}
