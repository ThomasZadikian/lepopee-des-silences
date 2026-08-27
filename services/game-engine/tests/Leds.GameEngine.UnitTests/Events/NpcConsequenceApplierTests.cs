using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Events;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events;

public sealed class NpcConsequenceApplierTests
{
    [Fact]
    public void ApplyDamage_ShouldIgnoreNonPositiveAmount()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var before = run.CurrentHp;

        NpcConsequenceApplier.ApplyDamage(run, 0);
        NpcConsequenceApplier.ApplyDamage(run, -5);

        run.CurrentHp.Should().Be(before);
    }

    [Fact]
    public void ApplyDamage_ShouldReduceVitalityForPositiveAmount()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var before = run.CurrentHp;

        NpcConsequenceApplier.ApplyDamage(run, 7);

        run.CurrentHp.Should().Be(before - 7);
    }

    [Theory]
    [InlineData(0, 0.01)]
    [InlineData(1, 0.01)]
    [InlineData(25, 0.25)]
    [InlineData(100, 0.50)]
    public void ApplyCurse_ShouldClampDifficultyDeltaAndPersistCurse(int severity, double expectedDelta)
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var definition = new CatalogCurseDefinitionSnapshot(
            "curse.test",
            "1.0.0",
            "Test curse",
            "Description",
            null,
            severity,
            "NextCombatOnly",
            null,
            "effectset.test");

        NpcConsequenceApplier.ApplyCurse(run, definition);

        run.ActiveCurses.Should().ContainSingle(c =>
            c.CurseDefinitionKey == "curse.test"
            && Math.Abs(c.DifficultyDelta - expectedDelta) < 0.0001);
        run.GetActiveModifiers(RunModifierType.NextCombatDifficultyMultiplier)
            .Should().ContainSingle(m => Math.Abs(m.Value - expectedDelta) < 0.0001);
    }
}
