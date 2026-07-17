using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// "Dévoration" — <see cref="Run.OnRoomEnteredWithoutCombat"/> drains 3% of max vitality
/// when <see cref="RunModifierType.RoomTraversalHpDrain"/> is active and the player has
/// just traversed a room without resolving any combat node in it.
/// </summary>
public sealed class RunRoomTraversalDrainTests
{
    private static RunModifier CreateDrainModifier() => RunModifier.Create(
        RunModifierType.RoomTraversalHpDrain,
        value: 1,
        RunModifierDuration.UntilRunEnds,
        sourceType: "PalaceLaw",
        sourceKey: "law-devoration");

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldDrainVitality_WhenTheModifierIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(CreateDrainModifier());
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore - (int)Math.Round(run.MaxHp * 0.03));
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldDoNothing_WhenTheModifierIsNotActive()
    {
        var run = TestGameEngineFactory.CreateRun();
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore);
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldNeverDropCurrentHpBelowOne()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AddRunModifier(CreateDrainModifier());
        run.ApplyVitalityLoss(run.CurrentHp - 1);
        run.CurrentHp.Should().Be(1);

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(1, because: "an out-of-combat drain must never kill the player directly.");
    }

    [Fact]
    public void OnRoomEnteredWithoutCombat_ShouldIgnoreConsumedModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();
        var modifier = CreateDrainModifier();
        run.AddRunModifier(modifier);
        modifier.Consume(DateTime.UtcNow);
        var hpBefore = run.CurrentHp;

        run.OnRoomEnteredWithoutCombat();

        run.CurrentHp.Should().Be(hpBefore);
    }
}
