using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantRuntimeStateCoverageTests
{
    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(1, -1, 0, 0, 0)]
    [InlineData(1, 0, -1, 0, 0)]
    [InlineData(1, 0, 0, -1, 0)]
    [InlineData(1, 0, 0, 0, -1)]
    public void Create_ShouldRejectNegativeResources(int vitality, int guard, int focus, int mana, int charge)
    {
        var act = () => CombatantRuntimeState.Create(vitality, guard, focus, mana, charge);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectManaAboveMax()
    {
        var act = () => CombatantRuntimeState.Create(10, 0, currentMana: 11, maxMana: 10);
        act.Should().Throw<DomainException>().WithMessage("*exceed max mana*");
    }

    [Fact]
    public void ApplyDamage_ShouldCoverGuardAndVitalityPaths()
    {
        var state = CombatantRuntimeState.Create(100, 20);
        state.ApplyDamage(10);
        state.CurrentGuard.Should().Be(10);
        state.CurrentVitality.Should().Be(100);

        state.ApplyDamage(30);
        state.CurrentGuard.Should().Be(0);
        state.CurrentVitality.Should().Be(80);

        FluentActions.Invoking(() => state.ApplyDamage(0)).Should().Throw<DomainException>();
        state.MarkDefeated();
        FluentActions.Invoking(() => state.ApplyDamage(1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void VitalityGuardHeal_ShouldCoverValidationAndDefeatedPaths()
    {
        var state = CombatantRuntimeState.Create(50, 0);
        FluentActions.Invoking(() => state.ApplyVitalityDamage(0)).Should().Throw<DomainException>();
        state.ApplyVitalityDamage(10);
        FluentActions.Invoking(() => state.GainGuard(0)).Should().Throw<DomainException>();
        state.GainGuard(5);
        FluentActions.Invoking(() => state.ApplyHeal(100, 0)).Should().Throw<DomainException>();
        state.ApplyHeal(100, 100);

        state.MarkDefeated();
        FluentActions.Invoking(() => state.ApplyVitalityDamage(1)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => state.GainGuard(1)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => state.ApplyHeal(100, 1)).Should().Throw<DomainException>();
    }

    [Fact]
    public void ResetGuardToBase_ShouldCoverAllBranches()
    {
        var low = CombatantRuntimeState.Create(100, 2);
        low.ResetGuardToBase(5);
        low.CurrentGuard.Should().Be(5);

        var high = CombatantRuntimeState.Create(100, 10);
        high.ResetGuardToBase(5);
        high.CurrentGuard.Should().Be(10);

        var defeated = CombatantRuntimeState.Create(0, 0);
        defeated.ResetGuardToBase(5);
        defeated.CurrentGuard.Should().Be(0);
    }

    [Fact]
    public void Revive_ShouldValidateAndClamp()
    {
        var alive = CombatantRuntimeState.Create(10, 0);
        FluentActions.Invoking(() => alive.Revive(100, 10)).Should().Throw<DomainException>();

        var defeated = CombatantRuntimeState.Create(0, 0);
        FluentActions.Invoking(() => defeated.Revive(100, 0)).Should().Throw<DomainException>();
        defeated.Revive(100, 150);
        defeated.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void ManaAndCharge_ShouldCoverValidationNoOpAndCaps()
    {
        var state = CombatantRuntimeState.Create(100, 0, currentMana: 5, currentCharge: 1, maxMana: 10);

        FluentActions.Invoking(() => state.GainMana(-1)).Should().Throw<DomainException>();
        state.GainMana(20);
        state.CurrentMana.Should().Be(10);
        state.SpendMana(0);
        state.SpendMana(50);
        state.CurrentMana.Should().Be(0);

        FluentActions.Invoking(() => state.GainCharge(-1)).Should().Throw<DomainException>();
        state.GainCharge(10);
        state.CurrentCharge.Should().Be(5);
        state.SpendCharge(0);
        state.SpendCharge(20);
        state.CurrentCharge.Should().Be(0);
    }

    [Fact]
    public void ThreatAndPowerfulHit_ShouldCoverThresholdAndConsumePaths()
    {
        var state = CombatantRuntimeState.Create(100, 0);
        state.AccrueThreat(0);
        state.AccrueThreat(5);
        state.ThreatValue.Should().Be(5);

        state.RecordDamageTaken(25, 0);
        state.RecordDamageTaken(0, 100);
        state.RecordDamageTaken(24, 100);
        state.ConsumePowerfulHitSinceLastAction().Should().BeFalse();

        state.RecordDamageTaken(25, 100);
        state.ConsumePowerfulHitSinceLastAction().Should().BeTrue();
        state.ConsumePowerfulHitSinceLastAction().Should().BeFalse();

        var attacker = Guid.NewGuid();
        state.RecordLastAttacker(attacker);
        state.LastAttackerId.Should().Be(attacker);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 0)]
    [InlineData(50, -1)]
    public void DebugSetVitals_ShouldRejectInvalidValues(int vitality, int guard)
    {
        var state = CombatantRuntimeState.Create(100, 0);
        FluentActions.Invoking(() => state.DebugSetVitals(100, vitality, guard))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void DebugSetVitals_ShouldClearGuardWhenDefeated()
    {
        var state = CombatantRuntimeState.Create(100, 0);
        state.DebugSetVitals(100, 50, 7);
        state.CurrentGuard.Should().Be(7);
        state.DebugSetVitals(100, 0, 7);
        state.CurrentGuard.Should().Be(0);
        state.IsDefeated.Should().BeTrue();
    }

    [Fact]
    public void Rehydrate_ShouldPreserveOptionalState()
    {
        var id = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var at = DateTime.UtcNow.AddMinutes(-1);
        var state = CombatantRuntimeState.Rehydrate(id, 12, 3, 4, 5, 1.5m, at, 9, attacker, 20, true);

        state.Id.Should().Be(id);
        state.LastAttackerId.Should().Be(attacker);
        state.MaxMana.Should().Be(20);
        state.TookPowerfulHitSinceLastAction.Should().BeTrue();
    }
}
