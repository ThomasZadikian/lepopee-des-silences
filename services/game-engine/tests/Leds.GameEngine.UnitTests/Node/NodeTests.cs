using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class NodeTests
{
    [Fact]
    public void Create_ShouldCreateAvailableNode_WhenInputIsValid()
    {
        var node = CreateMapNode();

        node.Id.Should().NotBeNull();
        node.EventType.Should().Be(NodeEventType.Combat);
        node.RiskLevel.Should().Be(10);
        node.RewardProfile.Should().Be("test-reward");
        node.Row.Should().Be(0);
        node.Lane.Should().Be(0);
        node.ParentNodeIds.Should().BeEmpty();
        node.IsBoss.Should().BeFalse();
        node.State.Should().Be(NodeState.Available);
        node.IsAvailable.Should().BeTrue();
        node.IsPlanned.Should().BeFalse();
        node.ChosenEventOptionId.Should().BeNull();
        node.HasChosenEventOption.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCreatePlannedChildNode_WhenParentIsProvided()
    {
        var parentNode = CreateMapNode();

        var childNode = CreateMapNode(
            row: 1,
            lane: 0,
            parentNodeIds: new[] { parentNode.Id },
            initialState: NodeState.Planned);

        childNode.Row.Should().Be(1);
        childNode.ParentNodeIds.Should().ContainSingle().Which.Should().Be(parentNode.Id);
        childNode.State.Should().Be(NodeState.Planned);
        childNode.IsPlanned.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldThrowDomainException_WhenRiskLevelIsOutOfRange(int riskLevel)
    {
        var act = () => CreateMapNode(riskLevel: riskLevel);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("MapNode risk level must be between 0 and 100.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenRewardProfileIsEmpty()
    {
        var act = () => CreateMapNode(rewardProfile: string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("MapNode reward profile is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenRowIsNegative()
    {
        var act = () => CreateMapNode(row: -1);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("MapNode row must be greater than or equal to 0.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenInitialNodeHasParent()
    {
        var parentNodeId = NodeId.New();

        var act = () => CreateMapNode(
            row: 0,
            parentNodeIds: new[] { parentNodeId });

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Initial row MapNodes cannot have parents.");
    }

    [Fact]
    public void Unlock_ShouldMakeNodeAvailable_WhenNodeIsPlanned()
    {
        var parentNode = CreateMapNode();

        var node = CreateMapNode(
            row: 1,
            lane: 0,
            parentNodeIds: new[] { parentNode.Id },
            initialState: NodeState.Planned);

        node.Unlock();

        node.State.Should().Be(NodeState.Available);
        node.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldThrowDomainException_WhenNodeIsNotPlanned()
    {
        var node = CreateMapNode();

        var act = node.Unlock;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a planned MapNode can be unlocked.");
    }

    [Fact]
    public void Select_ShouldMakeNodeSelected_WhenNodeIsAvailable()
    {
        var node = CreateMapNode();

        node.Select();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Select_ShouldThrowDomainException_WhenNodeIsNotAvailable()
    {
        var parentNode = CreateMapNode();

        var node = CreateMapNode(
            row: 1,
            lane: 0,
            parentNodeIds: new[] { parentNode.Id },
            initialState: NodeState.Planned);

        var act = node.Select;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only an available MapNode can be selected.");
    }

    [Fact]
    public void Resolve_ShouldMakeNodeResolved_WhenNodeIsSelected()
    {
        var node = CreateMapNode();

        node.Select();

        node.Resolve();

        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void Resolve_ShouldThrowDomainException_WhenNodeIsNotSelected()
    {
        var node = CreateMapNode();

        var act = node.Resolve;

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a selected MapNode can be resolved.");
    }

    [Fact]
    public void Lock_ShouldMakeAvailableNodeLocked()
    {
        var node = CreateMapNode();

        node.Lock();

        node.State.Should().Be(NodeState.Locked);
    }

    [Fact]
    public void Lock_ShouldKeepSelectedNodeSelected()
    {
        var node = CreateMapNode();

        node.Select();

        node.Lock();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Lock_ShouldKeepResolvedNodeResolved()
    {
        var node = CreateMapNode();

        node.Select();
        node.Resolve();

        node.Lock();

        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void MarkUnreachable_ShouldMakeAvailableNodeUnreachable()
    {
        var node = CreateMapNode();

        node.MarkUnreachable();

        node.State.Should().Be(NodeState.Unreachable);
    }

    [Fact]
    public void MarkUnreachable_ShouldKeepSelectedNodeSelected()
    {
        var node = CreateMapNode();

        node.Select();

        node.MarkUnreachable();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void ChooseEventOption_ShouldStoreChoiceId_WhenNodeIsResolved()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        node.ChooseEventOption("listen");

        node.ChosenEventOptionId.Should().Be("listen");
        node.HasChosenEventOption.Should().BeTrue();
    }

    [Fact]
    public void ChooseEventOption_ShouldTrimChoiceId_WhenChoiceIdContainsWhitespaces()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        node.ChooseEventOption("  listen  ");

        node.ChosenEventOptionId.Should().Be("listen");
        node.HasChosenEventOption.Should().BeTrue();
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenNodeIsNotResolved()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        var act = () => node.ChooseEventOption("listen");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a resolved MapNode can receive an event choice.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceIdIsEmpty()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        var act = () => node.ChooseEventOption(string.Empty);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event choice id is required.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceIdIsWhitespace()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        var act = () => node.ChooseEventOption("   ");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event choice id is required.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceWasAlreadyResolved()
    {
        var node = CreateMapNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();
        node.ChooseEventOption("listen");

        var act = () => node.ChooseEventOption("leave");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Current event choice has already been resolved.");
    }

    private static MapNode CreateMapNode(
        NodeEventType eventType = NodeEventType.Combat,
        int riskLevel = 10,
        string rewardProfile = "test-reward",
        int row = 0,
        int lane = 0,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        bool isBoss = false,
        NodeState initialState = NodeState.Available)
    {
        return MapNode.Create(
            eventType,
            riskLevel,
            rewardProfile,
            row,
            lane,
            parentNodeIds ?? Array.Empty<NodeId>(),
            isBoss,
            initialState);
    }
}