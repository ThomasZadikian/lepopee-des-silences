using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// "Déni permanent" (Erika, offre légendaire) — le joueur peut retirer une loi active du
/// Palais, une fois toutes les 10 rooms, s'il possède l'objet permanent correspondant.
/// </summary>
public sealed class RunLawDenialTests
{
    private static PalaceLaw CreateLaw(string key = "law-echo-v1") => PalaceLaw.Create(
        key, "Loi de l'Écho", "1.0.0",
        domains: [PalaceLawDomain.Combat],
        effects:
        [
            PalaceLawEffect.Create(
                RunModifierType.PermanentCombatDifficultyBonus,
                value: 0.10,
                RunModifierDuration.UntilRunEnds),
        ]);

    [Fact]
    public void CanUseLawDenial_ShouldBeFalse_WhenPlayerDoesNotOwnTheItem()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: false);

        run.CanUseLawDenial.Should().BeFalse();
    }

    [Fact]
    public void CanUseLawDenial_ShouldBeTrue_WhenEnabledAndNeverUsed()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);

        run.CanUseLawDenial.Should().BeTrue();
        run.LawDenialLastUsedRoomIndex.Should().BeNull();
    }

    [Fact]
    public void RemovePalaceLaw_ShouldRemoveTheLaw_AndConsumeItsModifiers()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);
        var law = CreateLaw();
        run.ActivatePalaceLaw(law);

        run.RemovePalaceLaw(law.Key);

        run.ActivePalaceLaws.Should().BeEmpty();
        run.RunModifiers.Should().OnlyContain(modifier => modifier.IsConsumed);
    }

    [Fact]
    public void RemovePalaceLaw_ShouldStartTheCooldown()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);
        var law = CreateLaw();
        run.ActivatePalaceLaw(law);

        run.RemovePalaceLaw(law.Key);

        run.LawDenialLastUsedRoomIndex.Should().Be(run.CurrentRoomIndex);
        run.CanUseLawDenial.Should().BeFalse(
            because: "the cooldown has not elapsed yet — CurrentRoomIndex has not advanced.");
    }

    [Fact]
    public void RemovePalaceLaw_ShouldThrow_WhenNotEnabled()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: false);
        var law = CreateLaw();
        run.ActivatePalaceLaw(law);

        var act = () => run.RemovePalaceLaw(law.Key);

        act.Should().Throw<DomainException>().WithMessage("*law-denial capability*");
    }

    [Fact]
    public void RemovePalaceLaw_ShouldThrow_WhenLawIsNotActive()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);

        var act = () => run.RemovePalaceLaw("law-nonexistent");

        act.Should().Throw<DomainException>().WithMessage("*No active palace law*");
    }

    [Fact]
    public void RemovePalaceLaw_ShouldThrow_WhenStillOnCooldown()
    {
        var run = TestGameEngineFactory.CreateRun(lawDenialEnabled: true);
        var lawA = CreateLaw("law-echo-v1");
        var lawB = CreateLaw("law-void-v1");
        run.ActivatePalaceLaw(lawA);
        run.ActivatePalaceLaw(lawB);

        run.RemovePalaceLaw(lawA.Key);
        var act = () => run.RemovePalaceLaw(lawB.Key);

        act.Should().Throw<DomainException>().WithMessage("*cooldown*");
        run.ActivePalaceLaws.Should().ContainSingle(activeLaw => activeLaw.Key == lawB.Key);
    }
}
