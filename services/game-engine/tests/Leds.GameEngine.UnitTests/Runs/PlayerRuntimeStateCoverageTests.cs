using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class PlayerRuntimeStateCoverageTests
{
    [Fact]
    public void Create_ShouldDefaultVitalityAndUnboundedManaCap()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], mana: 50);

        state.CurrentVitality.Should().Be(100);
        state.MaxMana.Should().Be(int.MaxValue);
        state.Mana.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldRejectNonPositiveMaxVitality(int maxVitality)
    {
        var action = () => PlayerRuntimeState.Create(maxVitality, [Skill()]);
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectNullOrEmptySkills()
    {
        (() => PlayerRuntimeState.Create(100, null!)).Should().Throw<DomainException>();
        (() => PlayerRuntimeState.Create(100, [])).Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectVitalityOrManaBeyondCaps()
    {
        (() => PlayerRuntimeState.Create(100, [Skill()], currentVitality: 101))
            .Should().Throw<DomainException>();
        (() => PlayerRuntimeState.Create(100, [Skill()], mana: 11, maxMana: 10))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void TakeDamage_ShouldConsumeGuardBeforeVitality()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], guard: 20);

        state.TakeDamage(10);
        state.Guard.Should().Be(10);
        state.CurrentVitality.Should().Be(100);

        state.TakeDamage(25);
        state.Guard.Should().Be(0);
        state.CurrentVitality.Should().Be(85);
    }

    [Fact]
    public void TakeDamage_ShouldClampAtZeroAndRejectInvalidOrDefeatedUse()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()]);
        (() => state.TakeDamage(0)).Should().Throw<DomainException>();

        state.TakeDamage(150);
        state.CurrentVitality.Should().Be(0);
        state.IsDefeated.Should().BeTrue();
        (() => state.TakeDamage(1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void LoseVitality_ShouldIgnoreNonPositiveAndRespectFloor()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()]);

        state.LoseVitality(0);
        state.CurrentVitality.Should().Be(100);
        state.LoseVitality(-10);
        state.CurrentVitality.Should().Be(100);
        state.LoseVitality(200, floor: 1);
        state.CurrentVitality.Should().Be(1);
    }

    [Fact]
    public void Heal_ShouldClampAndRejectInvalidOrDefeatedUse()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], currentVitality: 50);
        (() => state.Heal(0)).Should().Throw<DomainException>();
        state.Heal(75);
        state.CurrentVitality.Should().Be(100);

        var defeated = PlayerRuntimeState.Rehydrate(100, 0, 0, 0, 0, [Skill()]);
        (() => defeated.Heal(1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void GainGuard_ShouldValidateAndAccumulate()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()]);
        (() => state.GainGuard(0)).Should().Throw<DomainException>();
        state.GainGuard(12);
        state.Guard.Should().Be(12);

        var defeated = PlayerRuntimeState.Rehydrate(100, 0, 0, 0, 0, [Skill()]);
        (() => defeated.GainGuard(1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void ManaOperations_ShouldValidateBoundsAndClampGain()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], mana: 5, maxMana: 10);
        (() => state.SpendMana(-1)).Should().Throw<DomainException>();
        (() => state.SpendMana(6)).Should().Throw<DomainException>();
        state.SpendMana(3);
        state.Mana.Should().Be(2);

        (() => state.GainMana(-1)).Should().Throw<DomainException>();
        state.GainMana(100);
        state.Mana.Should().Be(10);
    }

    [Fact]
    public void ChargeOperations_ShouldValidateAndAccumulate()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], charge: 2);
        (() => state.SpendCharge(-1)).Should().Throw<DomainException>();
        (() => state.SpendCharge(3)).Should().Throw<DomainException>();
        state.SpendCharge(1);
        state.Charge.Should().Be(1);

        (() => state.GainCharge(-1)).Should().Throw<DomainException>();
        state.GainCharge(4);
        state.Charge.Should().Be(5);
    }

    [Fact]
    public void SyncFromCombat_ShouldClampEveryResource()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], maxMana: 20);
        state.SyncFromCombat(150, -10, 50, -3);

        state.CurrentVitality.Should().Be(100);
        state.Guard.Should().Be(0);
        state.Mana.Should().Be(20);
        state.Charge.Should().Be(0);

        state.SyncFromCombat(-5, 12, -4, 7);
        state.CurrentVitality.Should().Be(0);
        state.Guard.Should().Be(12);
        state.Mana.Should().Be(0);
        state.Charge.Should().Be(7);
    }

    [Fact]
    public void ReplaceSkills_ShouldValidateAndReplace()
    {
        var state = PlayerRuntimeState.Create(100, [Skill("skill.one")]);
        (() => state.ReplaceSkills(null!)).Should().Throw<DomainException>();
        (() => state.ReplaceSkills([])).Should().Throw<DomainException>();

        state.ReplaceSkills([Skill("skill.two")]);
        state.Skills.Should().ContainSingle().Which.Key.Should().Be("skill.two");
    }

    [Fact]
    public void ReplaceEffectiveStats_ShouldValidateAndTopUpResources()
    {
        var state = PlayerRuntimeState.Create(100, [Skill()], currentVitality: 20, mana: 5, maxMana: 10);
        (() => state.ReplaceEffectiveStats(0, 10, 1)).Should().Throw<DomainException>();

        state.ReplaceEffectiveStats(120, 30, 4);
        state.MaxVitality.Should().Be(120);
        state.CurrentVitality.Should().Be(120);
        state.MaxMana.Should().Be(30);
        state.Mana.Should().Be(30);
        state.Charge.Should().Be(4);
    }

    private static PlayerRuntimeSkill Skill(string key = "skill.test") =>
        PlayerRuntimeSkill.Create(
            key, "Skill", "Damage", "SingleEnemy", "Damage",
            manaCost: 0, chargeCost: 0, basePower: 10,
            emotionalRegister: "Neutral");
}
