using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTests
{
    private const string Seed = "seed-001";
    private const string GeneratorVersion = "gen-0.4.0";
    private const string MarkovMatrixVersion = "markov-0.2.0";

    [Fact]
    public void StartNew_ShouldCreateActiveRun_WithInitialRoomAndMetadata()
    {
        var playerId = Guid.NewGuid();
        var initialRoom = CreateInitialRoom();

        var run = Run.StartNew(
            playerId,
            Seed,
            GeneratorVersion,
            MarkovMatrixVersion,
            initialRoom,
            DateTimeOffset.UtcNow);

        run.Id.Value.Should().NotBeEmpty();
        run.PlayerId.Should().Be(playerId);
        run.Seed.Should().Be(Seed);
        run.GeneratorVersion.Should().Be(GeneratorVersion);
        run.MarkovMatrixVersion.Should().Be(MarkovMatrixVersion);
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
        run.CurrentRoom.Nodes.Should().ContainSingle(node => node.IsRoomBossNode);
        run.CurrentRoom.Nodes.Should().OnlyContain(node =>
            node.EventCount >= 1 && node.EventCount <= 4);
    }

    [Fact]
    public void RoomCreate_ShouldThrow_WhenRoomDoesNotContainBetweenSixAndTenNodes()
    {
        var roomType = RoomType.Threshold;
        var bossProfile = CreateBossProfile(roomType);

        var combatNode = Node.Create(
            NodeEventType.Combat,
            20,
            "combat-common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var bossNode = Node.Create(
            NodeEventType.RoomBoss,
            80,
            "room-boss",
            nodeDepth: 1,
            parentNodeId: combatNode.Id,
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        var nodes = new[]
        {
        combatNode,
        bossNode
    };

        var act = () => Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            nodes);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("A room must contain between 6 and 10 nodes.");
    }

    [Fact]
    public void ChooseNode_ShouldSelectRequestedNode_LockSiblings_AndMarkUnreachableBranches()
    {
        var run = CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        selectedNode.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);

        run.CurrentRoom.Nodes
            .Where(node => node.NodeDepth == run.CurrentRoom.CurrentNodeDepth &&
                           node.Id != selectedNode.Id)
            .Should()
            .OnlyContain(node => node.State == NodeState.Locked);

        run.CurrentRoom.Nodes
            .Where(node => node.NodeDepth > run.CurrentRoom.CurrentNodeDepth)
            .Should()
            .OnlyContain(node =>
                node.State == NodeState.Planned ||
                node.State == NodeState.Unreachable);

        run.CurrentRoom.Nodes
            .Where(node =>
                node.NodeDepth > run.CurrentRoom.CurrentNodeDepth &&
                IsReachableFrom(selectedNode.Id, node, run.CurrentRoom.Nodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Planned);

        run.CurrentRoom.Nodes
            .Where(node =>
                node.NodeDepth > run.CurrentRoom.CurrentNodeDepth &&
                !IsReachableFrom(selectedNode.Id, node, run.CurrentRoom.Nodes))
            .Should()
            .OnlyContain(node => node.State == NodeState.Unreachable);
    }

    [Fact]
    public void ChooseNode_ShouldThrow_WhenNodeWasAlreadySelectedAtCurrentDepth()
    {
        var run = CreateRun();

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
        var run = CreateRun();

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
        var run = CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);
        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);

        run.ProgressCurrentRoom();

        run.CurrentRoom.CurrentNodeDepth.Should().Be(1);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.AvailableNodes.Should().NotBeEmpty();
        run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.NodeDepth == 1);
        run.CurrentRoom.AvailableNodes.Should().OnlyContain(node => node.State == NodeState.Available);
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldCompleteRoom_AndSetRunRoomResolved_WhenRoomBossIsResolved()
    {
        var run = CreateRun();

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

        bossNode.IsRoomBossNode.Should().BeTrue();
        bossNode.EventTypes.Should().Contain(NodeEventType.RoomBoss);

        run.ChooseNode(bossNode.Id);
        run.ResolveCurrentEvent();

        bossNode.State.Should().Be(NodeState.Resolved);
        run.CurrentRoom.State.Should().Be(RoomState.Completed);
        run.Status.Should().Be(RunStatus.RoomResolved);
    }

    [Fact]
    public void Abandon_ShouldCloseRun()
    {
        var run = CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.Abandon(endedAt);

        run.Status.Should().Be(RunStatus.Abandoned);
        run.EndedAt.Should().Be(endedAt);
    }

    private static Run CreateRun()
    {
        return Run.StartNew(
            Guid.NewGuid(),
            Seed,
            GeneratorVersion,
            MarkovMatrixVersion,
            CreateInitialRoom(),
            DateTimeOffset.UtcNow);
    }

    private static Room CreateInitialRoom()
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");

        var combatNode = Node.Create(
            NodeEventType.Combat,
            20,
            "combat-common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var itemNode = Node.Create(
            NodeEventType.Item,
            15,
            "common",
            nodeDepth: 0,
            initialState: NodeState.Available);

        var restNode = Node.Create(
            NodeEventType.Rest,
            5,
            "healing-only",
            nodeDepth: 1,
            parentNodeId: combatNode.Id,
            initialState: NodeState.Planned);

        var npcNode = Node.Create(
            new[]
            {
            NodeEvent.Create(NodeEventType.Npc, 1)
            },
            8,
            "narrative-choice",
            nodeDepth: 1,
            parentNodeIds: new[]
            {
            combatNode.Id,
            itemNode.Id
            },
            isRoomBossNode: false,
            initialState: NodeState.Planned);

        var rareNode = Node.Create(
            NodeEventType.Rare,
            25,
            "rare",
            nodeDepth: 1,
            parentNodeId: itemNode.Id,
            initialState: NodeState.Planned);

        var bossNode = Node.Create(
            new[]
            {
            NodeEvent.Create(NodeEventType.RoomBoss, 1)
            },
            riskLevel: 80,
            rewardProfile: "room-boss",
            nodeDepth: 2,
            parentNodeIds: new[]
            {
            restNode.Id,
            npcNode.Id,
            rareNode.Id
            },
            isRoomBossNode: true,
            initialState: NodeState.Planned);

        return Room.Create(
            0,
            roomType,
            "Threshold",
            bossProfile,
            new[]
            {
            combatNode,
            itemNode,
            restNode,
            npcNode,
            rareNode,
            bossNode
            });
    }

    private static RoomBossProfile CreateBossProfile(RoomType roomType)
    {
        return RoomBossProfile.Create(
            "threshold-guardian",
            "Gardien du Seuil",
            roomType,
            "High");
    }
    private static bool IsReachableFrom(
    NodeId ancestorNodeId,
    Node node,
    IReadOnlyCollection<Node> allNodes)
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
        var run = CreateRun();

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
        var run = CreateRun();

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
        var run = CreateRun();

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
}