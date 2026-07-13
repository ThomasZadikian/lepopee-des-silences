using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class PlayerRuntimeStateTests
{
    private static PlayerRuntimeSkill CreateDefaultSkill()
    {
        return PlayerRuntimeSkill.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10);
    }

    [Fact]
    public void Create_ShouldInitializeWithFullVitality()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.MaxVitality.Should().Be(100);
        state.CurrentVitality.Should().Be(100);
        state.Guard.Should().Be(0);
        state.Mana.Should().Be(0);
        state.Charge.Should().Be(0);
        state.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldInitializeWithExplicitCurrentVitality()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()], currentVitality: 50);

        state.CurrentVitality.Should().Be(50);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxVitalityIsZero()
    {
        var act = () => PlayerRuntimeState.Create(0, [CreateDefaultSkill()]);

        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoSkills()
    {
        var act = () => PlayerRuntimeState.Create(100, []);

        act.Should().Throw<DomainException>().WithMessage("*at least one skill*");
    }

    [Fact]
    public void TakeDamage_ShouldReduceVitality()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.TakeDamage(30);

        state.CurrentVitality.Should().Be(70);
        state.IsDefeated.Should().BeFalse();
    }

    [Fact]
    public void TakeDamage_ShouldAbsorbWithGuardFirst()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);
        state.GainGuard(20);

        state.TakeDamage(30);

        state.Guard.Should().Be(0);
        state.CurrentVitality.Should().Be(90);
    }

    [Fact]
    public void TakeDamage_ShouldNotGoBelowZero()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.TakeDamage(150);

        state.CurrentVitality.Should().Be(0);
        state.IsDefeated.Should().BeTrue();
    }

    [Fact]
    public void Heal_ShouldNotExceedMaxVitality()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()], currentVitality: 80);

        state.Heal(50);

        state.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreValues()
    {
        var skill = CreateDefaultSkill();
        var state = PlayerRuntimeState.Rehydrate(100, 75, 10, 3, 2, [skill]);

        state.MaxVitality.Should().Be(100);
        state.CurrentVitality.Should().Be(75);
        state.Guard.Should().Be(10);
        state.Mana.Should().Be(3);
        state.Charge.Should().Be(2);
        state.Skills.Should().HaveCount(1);
    }

    [Fact]
    public void SyncFromCombat_ShouldUpdateValues()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.SyncFromCombat(65, 5, 2, 1);

        state.CurrentVitality.Should().Be(65);
        state.Guard.Should().Be(5);
        state.Mana.Should().Be(2);
        state.Charge.Should().Be(1);
    }

    [Fact]
    public void SyncFromCombat_ShouldClampToMaxVitality()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.SyncFromCombat(150, 0, 0, 0);

        state.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void Create_ShouldDefaultMaxManaToUncapped_WhenNotSpecified()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()]);

        state.MaxMana.Should().Be(int.MaxValue);

        state.GainMana(1_000_000);

        state.Mana.Should().Be(1_000_000);
    }

    [Fact]
    public void Create_ShouldSetMaxMana_WhenSpecified()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()], mana: 20, maxMana: 20);

        state.MaxMana.Should().Be(20);
        state.Mana.Should().Be(20);
    }

    [Fact]
    public void Create_ShouldThrow_WhenManaExceedsMaxMana()
    {
        var act = () => PlayerRuntimeState.Create(100, [CreateDefaultSkill()], mana: 30, maxMana: 20);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GainMana_ShouldClampToMaxMana()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()], mana: 15, maxMana: 20);

        state.GainMana(10);

        state.Mana.Should().Be(20);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreMaxMana()
    {
        var skill = CreateDefaultSkill();
        var state = PlayerRuntimeState.Rehydrate(100, 75, 10, 15, 2, [skill], maxMana: 20);

        state.MaxMana.Should().Be(20);

        state.GainMana(10);

        state.Mana.Should().Be(20);
    }

    [Fact]
    public void SyncFromCombat_ShouldClampManaToMaxMana()
    {
        var state = PlayerRuntimeState.Create(100, [CreateDefaultSkill()], maxMana: 20);

        state.SyncFromCombat(50, 0, 999, 0);

        state.Mana.Should().Be(20);
    }
}