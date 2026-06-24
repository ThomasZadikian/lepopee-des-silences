using FluentAssertions;
using Leds.GameEngine.Domain.Combats.Atb;

namespace Leds.GameEngine.UnitTests.Combats.Atb;

public sealed class AtbSchedulerTests
{
    private static readonly Guid A = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid B = new("00000000-0000-0000-0000-000000000002");

    private static AtbParticipant P(Guid id, int fill, int gauge = 0, long recoveryUntil = 0, int initiative = 0, bool active = true)
        => new(id, gauge, fill, recoveryUntil, initiative, active);

    [Fact]
    public void Advance_ShouldMakeTheFasterFillerActFirst()
    {
        var result = AtbScheduler.Advance([P(A, fill: 100), P(B, fill: 200)], currentTick: 0);

        result.NextActorId.Should().Be(B);
        result.ElapsedTicks.Should().Be(50);
        result.CurrentTick.Should().Be(50);
        result.Participants.Single(p => p.CombatantId == A).Gauge.Should().Be(5000);
        result.Participants.Single(p => p.CombatantId == B).Gauge.Should().Be(10000);
    }

    [Fact]
    public void Advance_ShouldDelayFilling_WhileInRecovery()
    {
        var result = AtbScheduler.Advance([P(A, fill: 200, recoveryUntil: 30), P(B, fill: 100)], currentTick: 0);

        result.NextActorId.Should().Be(A);
        result.ElapsedTicks.Should().Be(80);
        result.Participants.Single(p => p.CombatantId == A).Gauge.Should().Be(10000);
        result.Participants.Single(p => p.CombatantId == B).Gauge.Should().Be(8000);
    }

    [Fact]
    public void Advance_ShouldBreakTiesByInitiative()
    {
        var result = AtbScheduler.Advance([P(A, fill: 100, initiative: 5), P(B, fill: 100, initiative: 10)], currentTick: 0);

        result.NextActorId.Should().Be(B);
    }

    [Fact]
    public void Advance_ShouldCapGaugeAtChargeOverflow()
    {
        var result = AtbScheduler.Advance([P(A, fill: 100_000, gauge: 9000)], currentTick: 0);

        result.NextActorId.Should().Be(A);
        result.Participants.Single().Gauge.Should().Be(AtbConstants.ReadyThreshold + AtbConstants.MaxChargeOverflow);
    }

    [Fact]
    public void Advance_ShouldActImmediately_WhenAlreadyAtThreshold()
    {
        var result = AtbScheduler.Advance([P(A, fill: 100, gauge: AtbConstants.ReadyThreshold)], currentTick: 7);

        result.NextActorId.Should().Be(A);
        result.ElapsedTicks.Should().Be(0);
        result.CurrentTick.Should().Be(7);
    }

    [Fact]
    public void Advance_ShouldStall_WhenNoOneCanEverFill()
    {
        var result = AtbScheduler.Advance([P(A, fill: 0)], currentTick: 0);

        result.Stalled.Should().BeTrue();
        result.NextActorId.Should().BeNull();
    }

    [Fact]
    public void Advance_ShouldIgnoreInactiveParticipants()
    {
        var result = AtbScheduler.Advance([P(A, fill: 999, active: false), P(B, fill: 100)], currentTick: 0);

        result.NextActorId.Should().Be(B);
        result.Participants.Single(p => p.CombatantId == A).Gauge.Should().Be(0);
    }

    [Fact]
    public void Advance_ShouldBeDeterministic_ForSameInput()
    {
        var first = AtbScheduler.Advance([P(A, fill: 130), P(B, fill: 170)], currentTick: 3);
        var second = AtbScheduler.Advance([P(A, fill: 130), P(B, fill: 170)], currentTick: 3);

        first.NextActorId.Should().Be(second.NextActorId);
        first.ElapsedTicks.Should().Be(second.ElapsedTicks);
    }

    [Fact]
    public void Jitter_ShouldBeStableAndBounded()
    {
        var a = DeterministicAtbJitter.FactorPerMille("seed-1", tick: 12, key: "room", 800, 1200);
        var b = DeterministicAtbJitter.FactorPerMille("seed-1", tick: 12, key: "room", 800, 1200);

        a.Should().Be(b);
        a.Should().BeInRange(800, 1200);
    }
}