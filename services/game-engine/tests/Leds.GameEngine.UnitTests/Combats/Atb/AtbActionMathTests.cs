using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Atb;

namespace Leds.GameEngine.UnitTests.Combats.Atb;

public sealed class AtbActionMathTests
{
    [Fact]
    public void ChargeDamageMultiplier_ShouldBeOne_WhenNoOverflow()
    {
        AtbActionMath.ChargeDamageMultiplier(AtbConstants.ReadyThreshold).Should().Be(1.0);
        AtbActionMath.ChargeDamageMultiplier(5000).Should().Be(1.0);
    }

    [Fact]
    public void ChargeDamageMultiplier_ShouldStepUpWithOverflow_CappedAt1_5()
    {
        AtbActionMath.ChargeDamageMultiplier(AtbConstants.ReadyThreshold + 2_000).Should().Be(1.15);
        AtbActionMath.ChargeDamageMultiplier(AtbConstants.ReadyThreshold + 5_000).Should().Be(1.30);
        AtbActionMath.ChargeDamageMultiplier(AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow)
            .Should().Be(1.5);
    }

    [Fact]
    public void RecoveryTicks_ShouldScaleWithPower_AndBeMitigatedByRecoveryStat()
    {
        AtbActionMath.RecoveryTicks(basePower: 10, recoveryStat: 5).Should().Be(40);
        AtbActionMath.RecoveryTicks(basePower: 40, recoveryStat: 5).Should().Be(160);
        AtbActionMath.RecoveryTicks(basePower: 10, recoveryStat: 10).Should().Be(20);
        AtbActionMath.RecoveryTicks(basePower: 0, recoveryStat: 5).Should().Be(0);
    }

    [Fact]
    public void Interrupt_ShouldPushBackByBase_WhenTargetNotCharging()
    {
        AtbActionMath.Interrupt(5_000).Should().Be(5_000 - AtbCalibration.StaggerPushBase);
    }

    [Fact]
    public void Interrupt_ShouldPushHarder_WhenTargetIsCharging()
    {
        var charging = AtbConstants.ReadyThreshold + 2_000;
        AtbActionMath.Interrupt(charging).Should().Be(charging - AtbCalibration.StaggerPushVsCharging);
    }

    [Fact]
    public void Interrupt_ShouldNotGoNegative()
    {
        AtbActionMath.Interrupt(1_000).Should().Be(0);
    }

    [Fact]
    public void InitialGauge_ShouldStaggerByInitiative_AndStayBelowThreshold()
    {
        AtbActionMath.InitialGauge(initiative: 50, markovOpeningBias: 0).Should().Be(5_000);
        AtbActionMath.InitialGauge(initiative: 200, markovOpeningBias: 0).Should().Be(AtbConstants.ReadyThreshold - 1);
        AtbActionMath.InitialGauge(initiative: 0, markovOpeningBias: -5_000).Should().Be(0);
    }
}