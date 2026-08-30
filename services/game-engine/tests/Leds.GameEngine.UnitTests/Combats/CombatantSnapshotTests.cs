using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantSnapshotTests
{
    [Fact]
    public void Create_ShouldCreateCombatantWithFullHealth()
    {
        var combatant = CombatantSnapshot.Create(
            templateKey: "enemy-shadow-v1",
            displayName: "Shadow",
            CombatantSide.Enemy,
            maxHealth: 30,
            attack: 8,
            defense: 2,
            speed: 5);

        combatant.CurrentHealth.Should().Be(30);
        combatant.MaxHealth.Should().Be(30);
        combatant.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void ReceiveDamage_ShouldReduceCurrentHealth()
    {
        var combatant = CombatantSnapshot.Create(
            templateKey: "enemy-shadow-v1",
            displayName: "Shadow",
            CombatantSide.Enemy,
            maxHealth: 30,
            attack: 8,
            defense: 2,
            speed: 5);

        combatant.ReceiveDamage(12);

        combatant.CurrentHealth.Should().Be(18);
        combatant.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void ReceiveDamage_ShouldNotGoBelowZero()
    {
        var combatant = CombatantSnapshot.Create(
            templateKey: "enemy-shadow-v1",
            displayName: "Shadow",
            CombatantSide.Enemy,
            maxHealth: 30,
            attack: 8,
            defense: 2,
            speed: 5);

        combatant.ReceiveDamage(99);

        combatant.CurrentHealth.Should().Be(0);
        combatant.IsDefeated.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxHealthIsInvalid()
    {
        var act = () => CombatantSnapshot.Create(
            templateKey: "enemy-shadow-v1",
            displayName: "Shadow",
            CombatantSide.Enemy,
            maxHealth: 0,
            attack: 8,
            defense: 2,
            speed: 5);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Combatant max health must be greater than zero.");
    }
}