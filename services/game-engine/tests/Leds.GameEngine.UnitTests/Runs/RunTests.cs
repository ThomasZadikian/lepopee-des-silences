using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTests
{
    [Fact]
    public void StartNew_ShouldCreateActiveRun_WithInitialRoomAndMetadata()
    {
        var playerId = Guid.NewGuid();
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            playerId,
            "seed-001",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow);

        run.Id.Value.Should().NotBeEmpty();
        run.PlayerId.Should().Be(playerId);
        run.Seed.Should().Be("seed-001");
        run.GeneratorVersion.Should().Be("gen-0.4.0");
        run.MarkovMatrixVersion.Should().Be("markov-0.2.0");
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentDepth.Should().Be(0);

        run.CurrentRoom.Depth.Should().Be(0);
        run.CurrentRoom.RoomType.Should().Be(RoomType.Threshold);
        run.CurrentRoom.Theme.Should().Be("Threshold");
        run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        run.CurrentRoom.MaxNodeDepth.Should().Be(2);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.TotalNodeCount.Should().Be(6);

        run.CurrentRoom.AvailableNodes.Should().HaveCount(2);
        run.CurrentRoom.Nodes.Should().ContainSingle(node => node.IsBoss);
    }

    [Fact]
    public void StartNew_ShouldSeedPlayerStateManaAndCharge_FromArguments()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-mana",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            mana: 25,
            charge: 3);

        run.PlayerState!.Mana.Should().Be(25);
        run.PlayerState!.Charge.Should().Be(3);
    }

    [Fact]
    public void AppendJournalEntry_ShouldRecordText_WhenJournalEnabled()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-journal",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            journalEnabled: true);

        run.AppendJournalEntry("J'ai trouvé un objet abandonné dans la Pièce des émotions, c'était un carnet.");

        run.JournalEntries.Should().ContainSingle()
            .Which.Should().Be("J'ai trouvé un objet abandonné dans la Pièce des émotions, c'était un carnet.");
    }

    [Fact]
    public void AppendJournalEntry_ShouldBeNoOp_WhenJournalDisabled()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-no-journal",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow);

        run.AppendJournalEntry("Ceci ne devrait jamais être écrit.");

        run.JournalEntries.Should().BeEmpty();
    }

    [Fact]
    public void RoomCreate_ShouldThrow_WhenRoomDoesNotContainAtLeastTwoNodes()
    {
        var roomType = RoomType.Threshold;
        var bossProfile = CreateBossProfile(roomType);

        var singleNode = MapNode.Create(
            NodeEventType.Combat,
            20,
            "combat-common",
            row: 0,
            lane: 0,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available);

        var nodes = new[] { singleNode };

        var act = () => Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            nodes);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("A room must contain at least 2 nodes.");
    }

    [Fact]
    public void ChooseNode_ShouldSelectRequestedNode_LockSiblings_AndMarkUnreachableBranches()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        selectedNode.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);

        run.CurrentRoom.Nodes
            .Where(node => node.Row == run.CurrentRoom.CurrentNodeDepth &&
                           node.Id != selectedNode.Id)
            .Should()
            .OnlyContain(node => node.State == NodeState.Locked);

        run.CurrentRoom.Nodes
            .Where(node => node.Row > run.CurrentRoom.CurrentNodeDepth)
            .Should()
            .OnlyContain(node =>
                node.State == NodeState.Planned ||
                node.State == NodeState.Unreachable);

        run.CurrentRoom.Nodes
            .Where(node =>
                node.Row > run.CurrentRoom.CurrentNodeDepth &&
                IsReachableFrom(selectedNode.Id, node, run.CurrentRoom.Nodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Planned);

        run.CurrentRoom.Nodes
            .Where(node =>
                node.Row > run.CurrentRoom.CurrentNodeDepth &&
                !IsReachableFrom(selectedNode.Id, node, run.CurrentRoom.Nodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Unreachable);
    }

    [Fact]
    public void ChooseNode_ShouldThrow_WhenNodeWasAlreadySelectedAtCurrentDepth()
    {
        var run = TestGameEngineFactory.CreateRun();

        var firstNode = run.CurrentRoom.AvailableNodes.First();
        var secondNode = run.CurrentRoom.AvailableNodes.Last();

        run.ChooseNode(firstNode.Id);

        var act = () => run.ChooseNode(secondNode.Id);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room is not waiting for a node selection.");
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldResolveSelectedNode_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        selectedNode.State.Should().Be(NodeState.Resolved);
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public void ProgressCurrentRoom_ShouldUnlockNextLayer_AfterCurrentEventIsResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);

        run.ProgressCurrentRoom();

        run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();
        run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.Row == 1);
        run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldCompleteRoom_AndSetRunRoomResolved_WhenRoomBossIsResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        while (run.CurrentRoom.State != RoomState.BossReached)
        {
            var node = run.CurrentRoom.AvailableNodes.First();

            run.ChooseNode(node.Id);
            run.ResolveCurrentEvent();
            run.ProgressCurrentRoom();
        }

        run.CurrentRoom.State.Should().Be(RoomState.BossReached);
        run.CurrentRoom.AvailableNodes.Should().ContainSingle();

        var bossNode = run.CurrentRoom.AvailableNodes.Single();

        bossNode.IsBoss.Should().BeTrue();
        bossNode.EventType.Should().Be(NodeEventType.RoomBoss);

        run.ChooseNode(bossNode.Id);
        run.ResolveCurrentEvent();

        bossNode.State.Should().Be(NodeState.Resolved);
        run.CurrentRoom.State.Should().Be(RoomState.Completed);
        run.Status.Should().Be(RunStatus.RoomResolved);
    }

    [Fact]
    public void MoveToNextRoom_ShouldThrow_WhenRunIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.MoveToNextRoom(
            TestGameEngineFactory.CreateThresholdRoom(depth: 1));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is closed.");
    }

    [Fact]
    public void MoveToNextRoom_ShouldThrow_WhenRunIsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.MoveToNextRoom(
            TestGameEngineFactory.CreateThresholdRoom(depth: 1));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is closed.");
    }

    [Fact]
    public void CompleteRun_ShouldCloseRun_AsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.CompleteRun(endedAt);

        run.Status.Should().Be(RunStatus.Completed);
        run.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void CompleteRun_ShouldThrow_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.CompleteRun(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is already closed.");
    }

    [Fact]
    public void FailRun_ShouldCloseRun_AsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(3);

        run.FailRun(endedAt);

        run.Status.Should().Be(RunStatus.Failed);
        run.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void FailRun_ShouldThrow_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.FailRun(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is already closed.");
    }

    [Fact]
    public void Abandon_ShouldCloseRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.Abandon(endedAt);

        run.Status.Should().Be(RunStatus.Abandoned);
        run.EndedAt.Should().Be(endedAt);
    }

    private static RoomBossProfile CreateBossProfile(RoomType roomType)
    {
        return RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High",
            "boss-threshold-guardian-v1");
    }

    private static bool IsReachableFrom(
    NodeId ancestorNodeId,
    MapNode node,
    IReadOnlyCollection<MapNode> allNodes)
    {
        if (node.ParentNodeIds.Contains(ancestorNodeId))
        {
            return true;
        }

        foreach (var parentNodeId in node.ParentNodeIds)
        {
            var parent = allNodes.SingleOrDefault(candidate => candidate.Id == parentNodeId);

            if (parent is not null && IsReachableFrom(ancestorNodeId, parent, allNodes))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldAddActiveLaw_WhenRunIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[]
            {
            PalaceLawDomain.Narrative,
            PalaceLawDomain.Generation
            });

        run.ActivatePalaceLaw(law);

        run.ActivePalaceLaws.Should().ContainSingle();

        var activeLaw = run.ActivePalaceLaws.Single();

        activeLaw.Key.Should().Be("law-silence-v1");
        activeLaw.Name.Should().Be("Loi du Silence");
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldNotDuplicateLaw_WhenLawIsAlreadyActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative });

        run.ActivatePalaceLaw(law);
        run.ActivatePalaceLaw(law);

        run.ActivePalaceLaws.Should().ContainSingle();
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldThrowDomainException_WhenRunIsClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.Abandon(DateTimeOffset.UtcNow);

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative });

        var act = () => run.ActivatePalaceLaw(law);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Cannot activate a palace law on a closed run.");
    }

    // -----------------------------------------------------------------------
    // ApplyReward — MVP heal rewards
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyReward_ShouldHealPlayer_WhenRewardTypeIsHeal()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "reward-heal-seed",
            "gen-test",
            "markov-test",
            TestGameEngineFactory.CreateThresholdRoom(),
            DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 10);
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        run.ApplyReward(choice);

        run.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void ApplyReward_ShouldNotExceedMaxVitality()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "reward-heal-cap-seed",
            "gen-test",
            "markov-test",
            TestGameEngineFactory.CreateThresholdRoom(),
            DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 35);
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        run.ApplyReward(choice);

        run.CurrentHp.Should().Be(40);
    }

    [Fact]
    public void ApplyReward_ShouldThrow_WhenNoPendingReward()
    {
        var run = TestGameEngineFactory.CreateRun();
        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 10 PV.",
            "heal:10");

        var act = () => run.ApplyReward(choice);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run has no pending reward offer.");
    }

    [Fact]
    public void ApplyReward_ShouldThrow_WhenRewardTypeIsUnsupported()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.StatBonus,
            "Bonus d'attaque",
            "Non exposé dans le MVP.",
            "stat_bonus:attack:5");

        var act = () => run.ApplyReward(choice);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Reward type 'StatBonus' is not supported.");
    }

    [Fact]
    public void TryAddRunItem_ShouldAccept_UntilCapacityIsReached()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 2);

        var accepted1 = run.TryAddRunItem(CreateDistinctItem("item.a"));
        var accepted2 = run.TryAddRunItem(CreateDistinctItem("item.b"));

        accepted1.Should().BeTrue();
        accepted2.Should().BeTrue();
        run.RunItems.Should().HaveCount(2);
    }

    [Fact]
    public void TryAddRunItem_ShouldReject_WhenBagIsFull()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 2);

        run.TryAddRunItem(CreateDistinctItem("item.a"));
        run.TryAddRunItem(CreateDistinctItem("item.b"));

        var accepted3 = run.TryAddRunItem(CreateDistinctItem("item.c"));

        accepted3.Should().BeFalse();
        run.RunItems.Should().HaveCount(2);
    }

    [Fact]
    public void TryAddRunItem_ShouldStackIntoExistingItem_WithoutCountingAgainstCapacity()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 1);

        run.TryAddRunItem(CreateDistinctItem("item.a", quantity: 1));
        var accepted = run.TryAddRunItem(CreateDistinctItem("item.a", quantity: 1));

        accepted.Should().BeTrue();
        run.RunItems.Should().ContainSingle();
        run.RunItems.Single().Quantity.Should().Be(2);
    }

    private static RunItem CreateDistinctItem(string definitionKey, int quantity = 1) =>
        RunItem.Create(
            definitionKey,
            definitionKey,
            "Un objet de test.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity,
            RunItemEffectType.Heal,
            10);
}