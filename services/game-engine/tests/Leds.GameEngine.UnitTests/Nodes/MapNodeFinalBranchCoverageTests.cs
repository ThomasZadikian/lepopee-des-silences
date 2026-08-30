using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class MapNodeFinalBranchCoverageTests
{
    [Fact]
    public void Create_ShouldCoverRemainingValidationCombinations()
    {
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, riskLevel: -1));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, riskLevel: 101));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Item, combatRiskTier: RiskTier.Calme));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, rewardProfile: " "));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, row: -1));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, lane: -1));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, initialState: NodeState.Selected));
        Assert.Throws<DomainException>(() => MapNode.Create(
            NodeEventType.Combat, 10, "standard", 0, 0, null!));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Combat, row: 0, parents: [NodeId.New()]));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, isBoss: true));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.RoomBoss, isBoss: true, hiddenState: HiddenState.Hint));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Exit, hiddenState: HiddenState.Hint));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Item, exitDestinationRoomKey: "room.next"));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Item, exitDestinationDisplayName: "Next"));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Item, hiddenState: HiddenState.Revealed));
        Assert.Throws<DomainException>(() => Create(
            NodeEventType.Combat, dangerTell: DangerTell.Tracks, contactBehavior: ContactBehavior.None));

        Create(NodeEventType.RoomBoss, isBoss: true).IsBoss.Should().BeTrue();
        Create(NodeEventType.FinalBoss, isBoss: true).IsBoss.Should().BeTrue();
        Create(NodeEventType.Exit, exitDestinationRoomKey: "room.next", exitDestinationDisplayName: "Next")
            .ExitDestinationRoomKey.Should().Be("room.next");
        Create(NodeEventType.Combat, dangerTell: DangerTell.Glow, contactBehavior: ContactBehavior.TriggerOnEnter)
            .DangerTell.Should().Be(DangerTell.Glow);
    }

    [Fact]
    public void LifecycleAndRiskMethods_ShouldCoverNoOpGuardsErrorsAndSuccesses()
    {
        var parent = NodeId.New();
        var child = Create(NodeEventType.Combat, row: 1, parents: [parent]);
        child.AddParent(parent);
        child.ParentNodeIds.Should().ContainSingle();
        child.AddParent(NodeId.New());
        child.ParentNodeIds.Should().HaveCount(2);

        var planned = Create(NodeEventType.Combat, initialState: NodeState.Planned);
        planned.Unlock();
        planned.State.Should().Be(NodeState.Available);
        Assert.Throws<DomainException>(() => planned.Unlock());

        planned.Select();
        Assert.Throws<DomainException>(() => planned.Select());
        planned.Resolve();
        Assert.Throws<DomainException>(() => planned.Resolve());

        var movable = Create(NodeEventType.Combat, combatRiskTier: RiskTier.Calme);
        movable.MoveExplorationActorTo(1, 0);
        movable.Lane.Should().Be(1);
        Assert.Throws<DomainException>(() => movable.MoveExplorationActorTo(3, 0));
        Assert.Throws<DomainException>(() => movable.MoveExplorationActorTo(-1, 0));
        movable.Select();
        Assert.Throws<DomainException>(() => movable.MoveExplorationActorTo(2, 0));

        var locked = Create(NodeEventType.Combat);
        locked.Lock();
        locked.State.Should().Be(NodeState.Locked);
        locked.Lock();
        var plannedLock = Create(NodeEventType.Combat, initialState: NodeState.Planned);
        plannedLock.Lock();
        plannedLock.State.Should().Be(NodeState.Planned);
        var selectedLock = Create(NodeEventType.Combat);
        selectedLock.Select();
        selectedLock.Lock();
        selectedLock.State.Should().Be(NodeState.Selected);

        var unreachable = Create(NodeEventType.Combat);
        unreachable.MarkUnreachable();
        unreachable.State.Should().Be(NodeState.Unreachable);
        var selectedUnreachable = Create(NodeEventType.Combat);
        selectedUnreachable.Select();
        selectedUnreachable.MarkUnreachable();
        selectedUnreachable.State.Should().Be(NodeState.Selected);

        Assert.Throws<DomainException>(() => Create(NodeEventType.Item).RaiseRisk());
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat).RaiseRisk());
        var raiseLocked = Create(NodeEventType.Combat, combatRiskTier: RiskTier.Calme);
        raiseLocked.Lock();
        Assert.Throws<DomainException>(() => raiseLocked.RaiseRisk());
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat, combatRiskTier: RiskTier.Fatal).RaiseRisk());
        var raised = Create(NodeEventType.Combat, combatRiskTier: RiskTier.Calme);
        raised.RaiseRisk();
        raised.CombatRiskTier.Should().Be((RiskTier)((int)RiskTier.Calme + 1));

        Assert.Throws<DomainException>(() => Create(NodeEventType.Item).SetCombatRiskTier(RiskTier.Fatal));
        Assert.Throws<DomainException>(() => Create(NodeEventType.Combat).SetCombatRiskTier(RiskTier.Fatal));
        var setLocked = Create(NodeEventType.Combat, combatRiskTier: RiskTier.Calme);
        setLocked.Lock();
        Assert.Throws<DomainException>(() => setLocked.SetCombatRiskTier(RiskTier.Fatal));
        var set = Create(NodeEventType.Combat, combatRiskTier: RiskTier.Calme);
        set.SetCombatRiskTier(RiskTier.Fatal);
        set.CombatRiskTier.Should().Be(RiskTier.Fatal);

        Assert.Throws<DomainException>(() => Create(NodeEventType.Item).Reveal());
        var hidden = Create(NodeEventType.Item, hiddenState: HiddenState.Hint);
        hidden.Reveal();
        hidden.HiddenState.Should().Be(HiddenState.Revealed);
    }

    [Theory]
    [InlineData(NodeEventType.Combat, true)]
    [InlineData(NodeEventType.Rare, true)]
    [InlineData(NodeEventType.Elite, true)]
    [InlineData(NodeEventType.RoomBoss, true)]
    [InlineData(NodeEventType.FinalBoss, true)]
    [InlineData(NodeEventType.Item, false)]
    [InlineData(NodeEventType.Npc, false)]
    [InlineData(NodeEventType.Exit, false)]
    public void IsCombatFlavored_ShouldCoverEveryLogicalArm(NodeEventType eventType, bool expected)
    {
        MapNode.IsCombatFlavored(eventType).Should().Be(expected);
    }

    private static MapNode Create(
        NodeEventType type,
        int riskLevel = 10,
        string rewardProfile = "standard",
        int row = 0,
        int lane = 0,
        IReadOnlyCollection<NodeId>? parents = null,
        bool isBoss = false,
        NodeState initialState = NodeState.Available,
        RiskTier? combatRiskTier = null,
        HiddenState hiddenState = HiddenState.None,
        DangerTell dangerTell = DangerTell.None,
        ContactBehavior contactBehavior = ContactBehavior.None,
        string? exitDestinationRoomKey = null,
        string? exitDestinationDisplayName = null) =>
        MapNode.Create(
            type, riskLevel, rewardProfile, row, lane, parents ?? [], isBoss,
            initialState, combatRiskTier, hiddenState, dangerTell, contactBehavior,
            exitDestinationRoomKey, exitDestinationDisplayName);
}
