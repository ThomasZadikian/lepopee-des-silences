using FluentAssertions;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunPalaceLawImpactTests
{
    [Fact]
    public void ActivatePalaceLaw_ShouldAddRunModifiers_WhenLawHasEffects()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-echo-v1",
            "Loi de l'Écho",
            "1.0.0",
            domains: [PalaceLawDomain.Combat, PalaceLawDomain.Rewards],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.PermanentCombatDifficultyBonus,
                    value: 0.10,
                    RunModifierDuration.UntilRunEnds),
                PalaceLawEffect.Create(
                    RunModifierType.RewardPowerMultiplierBonus,
                    value: 0.15,
                    RunModifierDuration.UntilRunEnds),
            ]);

        run.ActivatePalaceLaw(law);

        var combatModifiers = run.GetActiveModifiers(RunModifierType.PermanentCombatDifficultyBonus);
        combatModifiers.Should().ContainSingle();
        combatModifiers.Single().Value.Should().Be(0.10);
        combatModifiers.Single().SourceType.Should().Be("PalaceLaw");
        combatModifiers.Single().SourceKey.Should().Be("law-echo-v1");

        var rewardModifiers = run.GetActiveModifiers(RunModifierType.RewardPowerMultiplierBonus);
        rewardModifiers.Should().ContainSingle();
        rewardModifiers.Single().Value.Should().Be(0.15);
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldNotAddModifiers_WhenLawHasNoEffects()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            domains: [PalaceLawDomain.Narrative]);

        run.ActivatePalaceLaw(law);

        run.RunModifiers.Should().BeEmpty();
        run.ActivePalaceLaws.Should().ContainSingle();
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldBeIdempotent_WhenSameLawActivatedTwice()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-echo-v1",
            "Loi de l'Écho",
            "1.0.0",
            domains: [PalaceLawDomain.Combat],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.PermanentCombatDifficultyBonus,
                    value: 0.10,
                    RunModifierDuration.UntilRunEnds),
            ]);

        run.ActivatePalaceLaw(law);
        run.ActivatePalaceLaw(law); // deuxième appel ignoré

        run.ActivePalaceLaws.Should().ContainSingle();
        run.RunModifiers.Should().ContainSingle(); // modifier non dupliqué
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldStackModifiers_WhenTwoDifferentLawsAreActivated()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law1 = PalaceLaw.Create(
            "law-echo-v1", "Loi de l'Écho", "1.0.0",
            domains: [PalaceLawDomain.Combat],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.PermanentCombatDifficultyBonus,
                    value: 0.10,
                    RunModifierDuration.UntilRunEnds),
            ]);

        var law2 = PalaceLaw.Create(
            "law-void-v1", "Loi du Vide", "1.0.0",
            domains: [PalaceLawDomain.Combat],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.PermanentCombatDifficultyBonus,
                    value: 0.10,
                    RunModifierDuration.UntilRunEnds),
            ]);

        run.ActivatePalaceLaw(law1);
        run.ActivatePalaceLaw(law2);

        run.ActivePalaceLaws.Should().HaveCount(2);
        run.GetActiveModifiers(RunModifierType.PermanentCombatDifficultyBonus)
            .Should().HaveCount(2); // les deux stackent
    }
}