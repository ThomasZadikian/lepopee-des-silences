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
        var act = () => Combatant.Create(CombatantId.New(), "", "Hero", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant source key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDisplayNameIsEmpty()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "", CombatantSide.Player, "Fighter", 100, 100, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant display name is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxVitalityIsZeroOrNegative()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 0, 0, 0, 0, 0, 0);

        act.Should().Throw<DomainException>().WithMessage("Combatant max vitality must be greater than zero.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCurrentVitalityExceedsMaxVitality()
    {
        var act = () => Combatant.Create(CombatantId.New(), "player.self", "Hero", CombatantSide.Player, "Fighter", 100, 150, 0, 0, 0, 0);

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

    [Fact]
    public void ApplyDamage_ShouldReduceVitality()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(12);

        combatant.CurrentVitality.Should().Be(68);
        combatant.Guard.Should().Be(0);
        combatant.Status.Should().Be(CombatantStatus.Active);
    }

    [Fact]
    public void ApplyDamage_ShouldConsumeGuardBeforeVitality()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        combatant.GainGuard(5);

        combatant.ApplyDamage(12);

        combatant.Guard.Should().Be(0);
        combatant.CurrentVitality.Should().Be(73);
    }

    [Fact]
    public void ApplyDamage_ShouldNotReduceVitalityBelowZero()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(120);

        combatant.CurrentVitality.Should().Be(0);
    }

    [Fact]
    public void ApplyDamage_ShouldMarkCombatantDefeated_WhenVitalityReachesZero()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        combatant.ApplyDamage(80);

        combatant.Status.Should().Be(CombatantStatus.Defeated);
        combatant.IsDefeated.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyDamage_ShouldThrow_WhenAmountIsZeroOrNegative(int amount)
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        var act = () => combatant.ApplyDamage(amount);

        act.Should().Throw<DomainException>().WithMessage("Damage amount must be greater than zero.");
    }

    [Fact]
    public void ApplyDamage_ShouldThrow_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        combatant.MarkDefeated();

        var act = () => combatant.ApplyDamage(1);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatants cannot receive damage.");
    }

    [Fact]
    public void GainGuard_ShouldIncreaseGuard()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        combatant.GainGuard(7);

        combatant.Guard.Should().Be(7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GainGuard_ShouldThrow_WhenAmountIsZeroOrNegative(int amount)
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);

        var act = () => combatant.GainGuard(amount);

        act.Should().Throw<DomainException>().WithMessage("Guard amount must be greater than zero.");
    }

    [Fact]
    public void GainGuard_ShouldThrow_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        combatant.MarkDefeated();

        var act = () => combatant.GainGuard(1);

        act.Should().Throw<DomainException>().WithMessage("Defeated combatants cannot gain guard.");
    }

    // -----------------------------------------------------------------------
    // BaseGuard / ResetGuardToBase
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateAlly_WithBaseGuard_ShouldSetBothGuardAndBaseGuard()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);

        combatant.Guard.Should().Be(8);
        combatant.BaseGuard.Should().Be(8);
    }

    [Fact]
    public void ResetGuardToBase_ShouldRestoreGuard_AfterDamageConsumedIt()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.ApplyDamage(5); // guard 8 → 3

        combatant.ResetGuardToBase();

        combatant.Guard.Should().Be(8,
            because: "Guard must be restored to BaseGuard at round start.");
    }

    [Fact]
    public void ResetGuardToBase_ShouldNotReduceGuard_WhenGainGuardExceedsBase()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.GainGuard(5); // guard = 13 > BaseGuard 8

        combatant.ResetGuardToBase();

        combatant.Guard.Should().Be(13,
            because: "ResetGuardToBase must not reduce guard earned via skill.basic.guard.");
    }

    [Fact]
    public void ResetGuardToBase_ShouldDoNothing_WhenCombatantIsDefeated()
    {
        var combatant = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, baseGuard: 8);
        combatant.MarkDefeated();

        var act = () => combatant.ResetGuardToBase();

        act.Should().NotThrow();
        combatant.Guard.Should().Be(8,
            because: "MarkDefeated does not reset guard; baseGuard value persists.");
    }
}