using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// "Calice infini" (John, butin légendaire) — le joueur peut restaurer 50% des PV max
/// d'une cible, une fois par Room, s'il possède l'objet permanent correspondant.
/// </summary>
public sealed class RunCaliceInfiniTests
{
    [Fact]
    public void CanUseCaliceInfini_ShouldBeFalse_WhenPlayerDoesNotOwnTheItem()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: false);

        run.CanUseCaliceInfini.Should().BeFalse();
    }

    [Fact]
    public void CanUseCaliceInfini_ShouldBeTrue_WhenEnabledAndNeverUsed()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: true);

        run.CanUseCaliceInfini.Should().BeTrue();
        run.CaliceInfiniLastUsedRoomIndex.Should().BeNull();
    }

    [Fact]
    public void UseCaliceInfini_ShouldHealThePlayerState_OutOfCombat()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: true);
        run.PlayerState.TakeDamage(run.PlayerState.MaxVitality - 1);

        run.UseCaliceInfini(targetCombatantId: null);

        run.PlayerState.CurrentVitality.Should().Be(
            Math.Min(run.PlayerState.MaxVitality, 1 + (int)Math.Round(run.PlayerState.MaxVitality * 0.5)));
    }

    [Fact]
    public void UseCaliceInfini_ShouldStartTheCooldown()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: true);

        run.UseCaliceInfini(targetCombatantId: null);

        run.CaliceInfiniLastUsedRoomIndex.Should().Be(run.CurrentRoomIndex);
        run.CanUseCaliceInfini.Should().BeFalse(
            because: "the cooldown has not elapsed yet — CurrentRoomIndex has not advanced.");
    }

    [Fact]
    public void UseCaliceInfini_ShouldThrow_WhenNotEnabled()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: false);

        var act = () => run.UseCaliceInfini(targetCombatantId: null);

        act.Should().Throw<DomainException>().WithMessage("*Calice infini capability*");
    }

    [Fact]
    public void UseCaliceInfini_ShouldThrow_WhenStillOnCooldown()
    {
        var run = TestGameEngineFactory.CreateRun(caliceInfiniEnabled: true);

        run.UseCaliceInfini(targetCombatantId: null);
        var act = () => run.UseCaliceInfini(targetCombatantId: null);

        act.Should().Throw<DomainException>().WithMessage("*cooldown*");
    }
}
