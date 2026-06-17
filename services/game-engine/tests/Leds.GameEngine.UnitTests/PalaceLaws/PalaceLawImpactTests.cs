using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
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

    [Fact]
    public void ActivatePalaceLaw_ShouldReplacePreviousRoomClimateLaw()
    {
        var run = TestGameEngineFactory.CreateRun();

        var heatwaveLaw = PalaceLaw.Create(
            "law-drought-v1",
            "Loi de la Canicule",
            "1.0.0",
            domains: [PalaceLawDomain.Combat],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.RoomClimate,
                    value: 3,
                    RunModifierDuration.UntilRoomEnds),
                PalaceLawEffect.Create(
                    RunModifierType.AttackPowerBonus,
                    value: 0.20,
                    RunModifierDuration.UntilRoomEnds),
            ]);

        var hailLaw = PalaceLaw.Create(
            "law-hail-v1",
            "Loi de la Grêle",
            "1.0.0",
            domains: [PalaceLawDomain.Combat],
            effects:
            [
                PalaceLawEffect.Create(
                    RunModifierType.RoomClimate,
                    value: 4,
                    RunModifierDuration.UntilRoomEnds),
            ]);

        run.ActivatePalaceLaw(heatwaveLaw);
        run.ActivatePalaceLaw(hailLaw);

        run.ActivePalaceLaws.Should().ContainSingle();
        run.ActivePalaceLaws.Single().Key.Should().Be("law-hail-v1");

        run.GetActiveModifiers(RunModifierType.RoomClimate).Should().ContainSingle();
        run.GetActiveModifiers(RunModifierType.RoomClimate).Single().Value.Should().Be(4);
        run.GetActiveModifiers(RunModifierType.AttackPowerBonus).Should().BeEmpty();

        run.RunModifiers
            .Where(modifier => modifier.SourceKey == "law-drought-v1")
            .Should().OnlyContain(modifier => modifier.IsConsumed);

        var dto = RunDto.FromDomain(run);
        dto.ActivePalaceLaws.Should().ContainSingle(law => law.Key == "law-hail-v1");
        dto.CurrentRoom.ActiveClimate.Should().Be("Hail");
        dto.ActiveModifiers.Should().NotBeNull();
        dto.ActiveModifiers!.Should().OnlyContain(modifier => modifier.SourceKey != "law-drought-v1");
    }
}
