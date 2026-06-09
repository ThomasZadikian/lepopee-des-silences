using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs.ExitMidRoom;

public sealed class ExitMidRoomTests
{
    private static Run CreateRunWithSomeProgression()
    {
        var run = TestGameEngineFactory.CreateRun();

        var firstNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(firstNode.Id);
        run.ResolveCurrentEvent();
        run.ProgressCurrentRoom();

        return run;
    }

    private static Run CreateRunWithMemoryFragmentAndPalaceLaw()
    {
        var run = CreateRunWithSomeProgression();

        run.AddMemoryFragment("memento-alpha");
        run.AddMemoryFragment("memento-beta");

        var law = PalaceLaw.Create(
            "law-silence-v1", "Loi du Silence", "1.0.0",
            [PalaceLawDomain.Narrative]);
        run.ActivatePalaceLaw(law);

        return run;
    }

    [Fact]
    public void ExitMidRoom_ShouldSetStatusToSuspended()
    {
        var run = CreateRunWithSomeProgression();

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Suspended);
    }

    [Fact]
    public void ExitMidRoom_ShouldSetSavedAt()
    {
        var run = CreateRunWithSomeProgression();
        var savedAt = new DateTimeOffset(2026, 6, 9, 12, 0, 0, TimeSpan.Zero);

        run.ExitMidRoom(savedAt);

        run.SavedAt.Should().Be(savedAt);
    }

    [Fact]
    public void ExitMidRoom_ShouldNotIncrementCurrentRoomIndex()
    {
        var run = CreateRunWithSomeProgression();
        var indexBefore = run.CurrentRoomIndex;

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.CurrentRoomIndex.Should().Be(indexBefore);
    }

    [Fact]
    public void ExitMidRoom_ShouldResetCurrentRoomProgress()
    {
        var run = TestGameEngineFactory.CreateRun();
        var firstNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(firstNode.Id);
        run.ResolveCurrentEvent();
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
        run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        run.CurrentRoom.Nodes.Should().Contain(n => n.State == NodeState.Selected || n.State == NodeState.Resolved);

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.AvailableNodes.Should().OnlyContain(n => n.Row == 0);
    }

    [Fact]
    public void ExitMidRoom_ShouldNotLeaveAnyNodeSelectedOrResolved()
    {
        var run = CreateRunWithSomeProgression();

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.CurrentRoom.Nodes.Should().NotContain(n =>
            n.State == NodeState.Selected || n.State == NodeState.Resolved);
    }

    [Fact]
    public void ExitMidRoom_ShouldRollbackStats()
    {
        var run = TestGameEngineFactory.CreateRun();
        var initialAttack = run.Attack;
        var initialDefense = run.Defense;
        var initialSpeed = run.Speed;
        run.ApplyStatBonus("attack", 5);
        run.ApplyStatBonus("defense", 3);
        run.Attack.Should().Be(initialAttack + 5);
        run.Defense.Should().Be(initialDefense + 3);

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.Attack.Should().Be(initialAttack);
        run.Defense.Should().Be(initialDefense);
        run.Speed.Should().Be(initialSpeed);
    }

    [Fact]
    public void ExitMidRoom_ShouldRollbackMemoryFragments()
    {
        var run = CreateRunWithMemoryFragmentAndPalaceLaw();
        run.MemoryFragments.Should().HaveCount(2);

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.MemoryFragments.Should().BeEmpty();
    }

    [Fact]
    public void ExitMidRoom_ShouldRollbackActivePalaceLaws()
    {
        var run = CreateRunWithMemoryFragmentAndPalaceLaw();
        run.ActivePalaceLaws.Should().ContainSingle();

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public void ExitMidRoom_ShouldBeResumableFromActive()
    {
        var run = CreateRunWithSomeProgression();

        run.ExitMidRoom(DateTimeOffset.UtcNow);

        run.Status.Should().Be(RunStatus.Suspended);

        run.Resume();

        run.Status.Should().Be(RunStatus.Active);
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunHasActiveCombat()
    {
        var run = CreateRunWithSomeProgression();
        run.SetActiveCombat(new CombatId(Guid.NewGuid()));

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*active combat*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunHasPendingRewardOffer()
    {
        var run = CreateRunWithSomeProgression();
        run.SetPendingRewardOffer(new RewardOfferId(Guid.NewGuid()));

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*pending reward offer*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsAlreadySuspended()
    {
        var run = CreateRunWithSomeProgression();
        run.ExitMidRoom(DateTimeOffset.UtcNow);

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsAbandoned()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.Abandon(DateTimeOffset.UtcNow);

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsRoomResolved()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.Status.Should().Be(RunStatus.RoomResolved);

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*must be active*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsInterlude()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*must be active*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void ExitMidRoom_ShouldFail_WhenRunIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.ExitMidRoom(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void ExitMidRoom_CanBeCalledAgain_AfterResume()
    {
        var run = CreateRunWithSomeProgression();

        run.ExitMidRoom(DateTimeOffset.UtcNow);
        run.Resume();

        run.Status.Should().Be(RunStatus.Active);

        var node = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(node.Id);
        run.ApplyStatBonus("attack", 10);

        var exitAgain = () => run.ExitMidRoom(DateTimeOffset.UtcNow);
        exitAgain.Should().NotThrow();

        run.Status.Should().Be(RunStatus.Suspended);
    }
}
