using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Stat-point mid-run resync ("Valider les choix") — <see cref="Run.ReplacePlayerStats"/>
/// (protagonist) and <see cref="Run.ReplaceCharacterStats"/> (companions).
/// </summary>
public sealed class RunReplaceStatsTests
{
    [Fact]
    public void ReplacePlayerStats_ShouldUpdateRunAndPlayerState()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.ReplacePlayerStats(
            maxVitality: 150, maxMana: 40, charge: 6,
            attack: 20, defense: 10, speed: 15, focus: 4,
            magicAttack: 8, magicDefense: 7);

        run.MaxHp.Should().Be(150);
        run.CurrentHp.Should().Be(150);
        run.Attack.Should().Be(20);
        run.Defense.Should().Be(10);
        run.Speed.Should().Be(15);
        run.Focus.Should().Be(4);
        run.MagicAttack.Should().Be(8);
        run.MagicDefense.Should().Be(7);

        run.PlayerState.MaxVitality.Should().Be(150);
        run.PlayerState.CurrentVitality.Should().Be(150);
        run.PlayerState.MaxMana.Should().Be(40);
        run.PlayerState.Mana.Should().Be(40);
        run.PlayerState.Charge.Should().Be(6);
    }

    [Fact]
    public void ReplacePlayerStats_ShouldThrow_WhenMaxVitalityIsNotPositive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.ReplacePlayerStats(
            maxVitality: 0, maxMana: 40, charge: 6,
            attack: 20, defense: 10, speed: 15, focus: 4,
            magicAttack: 8, magicDefense: 7);

        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Fact]
    public void ReplaceCharacterStats_ShouldReplaceTheMatchingCharacterStatBlock()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var character = run.PlayerSnapshot!.Characters.First();

        run.ReplaceCharacterStats(
            character.CharacterId,
            maxVitality: 120, attackPower: 15, defense: 8, startingGuard: 3,
            speed: 12, initiative: 11,focus: 2, mana: 10, charge: 1,
            magicAttack: 5, magicDefense: 4);

        character.StatBlock.MaxVitality.Should().Be(120);
        character.StatBlock.AttackPower.Should().Be(15);
        character.StatBlock.Defense.Should().Be(8);
        character.StatBlock.StartingGuard.Should().Be(3);
        character.StatBlock.Speed.Should().Be(12);
        character.StatBlock.Initiative.Should().Be(11);
        character.StatBlock.Focus.Should().Be(2);
        character.StatBlock.Mana.Should().Be(10);
        character.StatBlock.Charge.Should().Be(1);
        character.StatBlock.MagicAttack.Should().Be(5);
        character.StatBlock.MagicDefense.Should().Be(4);
    }

    [Fact]
    public void ReplaceCharacterStats_ShouldThrow_WhenCharacterIsNotInTheSnapshot()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();

        var act = () => run.ReplaceCharacterStats(
            Guid.NewGuid(),
            maxVitality: 120, attackPower: 15, defense: 8, startingGuard: 3,
            speed: 12, initiative: 11,focus: 2, mana: 10, charge: 1,
            magicAttack: 5, magicDefense: 4);

        act.Should().Throw<DomainException>().WithMessage("*was not found*");
    }
}
