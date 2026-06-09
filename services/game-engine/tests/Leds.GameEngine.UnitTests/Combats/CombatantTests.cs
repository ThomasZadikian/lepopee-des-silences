using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantTests
{
    [Fact]
    public void CreateAlly_ShouldSucceed_WithValidData()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.SourceKey.Should().Be("player.self");
        combatant.DisplayName.Should().Be("Hero");
        combatant.Side.Should().Be(CombatantSide.Player);
        combatant.Archetype.Should().Be("Fighter");
        combatant.MaxVitality.Should().Be(100);
        combatant.CurrentVitality.Should().Be(100);
        combatant.Guard.Should().Be(0);
        combatant.Mana.Should().Be(0);
        combatant.Charge.Should().Be(0);
        combatant.Status.Should().Be(CombatantStatus.Active);
        combatant.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void CreateEnemy_ShouldSucceed_WithValidData()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.SourceKey.Should().Be("enemy.sentinel");
        combatant.DisplayName.Should().Be("Sentinel");
        combatant.Side.Should().Be(CombatantSide.Enemy);
        combatant.Archetype.Should().Be("Guard");
        combatant.MaxVitality.Should().Be(80);
        combatant.CurrentVitality.Should().Be(80);
        combatant.Status.Should().Be(CombatantStatus.Active);
    }

    [Fact]
    public void Create_ShouldThrow_WhenSourceKeyIsEmpty()
    {
        var act = () => Combatant.Create(CombatantId.New(), "", "Hero", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant source key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDisplayNameIsEmpty()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant display name is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxVitalityIsZeroOrNegative()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 0, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant max vitality must be greater than zero.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCurrentVitalityExceedsMaxVitality()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 100, 150, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant current vitality must be between zero and max vitality.");
    }

    [Fact]
    public void MarkDefeated_ShouldSetStatusDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.MarkDefeated();

        combatant.Status.Should().Be(CombatantStatus.Defeated);
        combatant.IsDefeated.Should().BeTrue();
        combatant.CurrentVitality.Should().Be(0);
    }

    [Fact]
    public void MarkDefeated_ShouldThrow_WhenAlreadyDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.MarkDefeated();

        var act = () => combatant.MarkDefeated();

        act.Should().Throw<DomainException>().WithMessage("Combatant is already defeated.");
    }
}
