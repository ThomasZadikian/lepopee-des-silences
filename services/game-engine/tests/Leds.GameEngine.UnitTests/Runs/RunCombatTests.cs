using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunCombatTests
{
    private static Combat CreateActiveCombat(RunId runId)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        return Combat.Create(
            CombatId.New(),
            runId,
            RoomId.New(),
            NodeId.New(),
            new[] { ally },
            new[] { enemy });
    }

    private static Run CreateRun()
    {
        return TestGameEngineFactory.CreateRun();
    }

    [Fact]
    public void StartCombat_ShouldAttachCombatToRun()
    {
        var run = CreateRun();
        var combat = CreateActiveCombat(run.Id);

        run.StartCombat(combat);

        run.ActiveCombat.Should().BeSameAs(combat);
    }

    [Fact]
    public void StartCombat_ShouldSetHasActiveCombat()
    {
        var run = CreateRun();
        var combat = CreateActiveCombat(run.Id);

        run.StartCombat(combat);

        run.HasActiveCombat.Should().BeTrue();
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenCombatIsNull()
    {
        var run = CreateRun();

        var act = () => run.StartCombat(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenRunIsNotActive()
    {
        var run = CreateRun();
        run.CompleteRun(DateTimeOffset.UtcNow);
        var combat = CreateActiveCombat(run.Id);

        var act = () => run.StartCombat(combat);

        act.Should().Throw<DomainException>().WithMessage("Run must be active to start a combat.");
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenRunAlreadyHasActiveCombat()
    {
        var run = CreateRun();
        var combat1 = CreateActiveCombat(run.Id);
        run.StartCombat(combat1);

        var combat2 = CreateActiveCombat(run.Id);

        var act = () => run.StartCombat(combat2);

        act.Should().Throw<DomainException>().WithMessage("Run already has an active combat.");
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenCombatBelongsToAnotherRun()
    {
        var run = CreateRun();
        var otherRunId = RunId.New();
        var combat = CreateActiveCombat(otherRunId);

        var act = () => run.StartCombat(combat);

        act.Should().Throw<DomainException>().WithMessage("Combat does not belong to this run.");
    }

    [Fact]
    public void StartCombat_ShouldThrow_WhenCombatIsNotActive()
    {
        var run = CreateRun();
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(), run.Id, RoomId.New(), NodeId.New(),
            new[] { ally }, new[] { enemy });
        combat.MarkCompleted();

        var act = () => run.StartCombat(combat);

        act.Should().Throw<DomainException>().WithMessage("Combat must be active to be started.");
    }
}