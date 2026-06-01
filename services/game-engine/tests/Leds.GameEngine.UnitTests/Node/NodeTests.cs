using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class NodeTests
{
    [Fact]
    public void Create_ShouldCreateAvailableNode_WhenInputIsValid()
    {
        // Act
        var node = CreateNode();

        // Assert
        node.Id.Should().NotBeNull();
        node.EventType.Should().Be(NodeEventType.Combat);
        node.EventTypes.Should().ContainSingle().Which.Should().Be(NodeEventType.Combat);
        node.EventCount.Should().Be(1);
        node.RiskLevel.Should().Be(10);
        node.RewardProfile.Should().Be("test-reward");
        node.NodeDepth.Should().Be(0);
        node.ParentNodeId.Should().BeNull();
        node.ParentNodeIds.Should().BeEmpty();
        node.IsRoomBossNode.Should().BeFalse();
        node.State.Should().Be(NodeState.Available);
        node.IsAvailable.Should().BeTrue();
        node.IsPlanned.Should().BeFalse();
        node.ChosenEventOptionId.Should().BeNull();
        node.HasChosenEventOption.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCreatePlannedChildNode_WhenParentIsProvided()
    {
        // Arrange
        var parentNode = CreateNode();

        // Act
        var childNode = CreateNode(
            nodeDepth: 1,
            parentNodeId: parentNode.Id,
            initialState: NodeState.Planned);

        // Assert
        childNode.NodeDepth.Should().Be(1);
        childNode.ParentNodeId.Should().Be(parentNode.Id);
        childNode.ParentNodeIds.Should().ContainSingle().Which.Should().Be(parentNode.Id);
        childNode.State.Should().Be(NodeState.Planned);
        childNode.IsPlanned.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldThrowDomainException_WhenRiskLevelIsOutOfRange(int riskLevel)
    {
        // Act
        var act = () => CreateNode(riskLevel: riskLevel);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Node risk level must be between 0 and 100.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenRewardProfileIsEmpty()
    {
        // Act
        var act = () => CreateNode(rewardProfile: string.Empty);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Node reward profile is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNodeDepthIsNegative()
    {
        // Act
        var act = () => CreateNode(nodeDepth: -1);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Node depth must be greater than or equal to 0.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenInitialNodeHasParent()
    {
        // Arrange
        var parentNodeId = NodeId.New();

        // Act
        var act = () => CreateNode(
            nodeDepth: 0,
            parentNodeId: parentNodeId);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Initial layer nodes cannot have parents.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNonInitialNodeHasNoParent()
    {
        // Act
        var act = () => CreateNode(
            nodeDepth: 1,
            parentNodeId: null,
            initialState: NodeState.Planned);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Non-initial nodes must have at least one parent.");
    }

    [Fact]
    public void Unlock_ShouldMakeNodeAvailable_WhenNodeIsPlanned()
    {
        // Arrange
        var parentNode = CreateNode();

        var node = CreateNode(
            nodeDepth: 1,
            parentNodeId: parentNode.Id,
            initialState: NodeState.Planned);

        // Act
        node.Unlock();

        // Assert
        node.State.Should().Be(NodeState.Available);
        node.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldThrowDomainException_WhenNodeIsNotPlanned()
    {
        // Arrange
        var node = CreateNode();

        // Act
        var act = node.Unlock;

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a planned node can be unlocked.");
    }

    [Fact]
    public void Select_ShouldMakeNodeSelected_WhenNodeIsAvailable()
    {
        // Arrange
        var node = CreateNode();

        // Act
        node.Select();

        // Assert
        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Select_ShouldThrowDomainException_WhenNodeIsNotAvailable()
    {
        // Arrange
        var parentNode = CreateNode();

        var node = CreateNode(
            nodeDepth: 1,
            parentNodeId: parentNode.Id,
            initialState: NodeState.Planned);

        // Act
        var act = node.Select;

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only an available node can be selected.");
    }

    [Fact]
    public void Resolve_ShouldMakeNodeResolved_WhenNodeIsSelected()
    {
        // Arrange
        var node = CreateNode();

        node.Select();

        // Act
        node.Resolve();

        // Assert
        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void Resolve_ShouldThrowDomainException_WhenNodeIsNotSelected()
    {
        // Arrange
        var node = CreateNode();

        // Act
        var act = node.Resolve;

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a selected node can be resolved.");
    }

    [Fact]
    public void Lock_ShouldMakeAvailableNodeLocked()
    {
        // Arrange
        var node = CreateNode();

        // Act
        node.Lock();

        // Assert
        node.State.Should().Be(NodeState.Locked);
    }

    [Fact]
    public void Lock_ShouldKeepSelectedNodeSelected()
    {
        // Arrange
        var node = CreateNode();

        node.Select();

        // Act
        node.Lock();

        // Assert
        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Lock_ShouldKeepResolvedNodeResolved()
    {
        // Arrange
        var node = CreateNode();

        node.Select();
        node.Resolve();

        // Act
        node.Lock();

        // Assert
        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void MarkUnreachable_ShouldMakeAvailableNodeUnreachable()
    {
        // Arrange
        var node = CreateNode();

        // Act
        node.MarkUnreachable();

        // Assert
        node.State.Should().Be(NodeState.Unreachable);
    }

    [Fact]
    public void MarkUnreachable_ShouldKeepSelectedNodeSelected()
    {
        // Arrange
        var node = CreateNode();

        node.Select();

        // Act
        node.MarkUnreachable();

        // Assert
        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void ChooseEventOption_ShouldStoreChoiceId_WhenNodeIsResolved()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        // Act
        node.ChooseEventOption("listen");

        // Assert
        node.ChosenEventOptionId.Should().Be("listen");
        node.HasChosenEventOption.Should().BeTrue();
    }

    [Fact]
    public void ChooseEventOption_ShouldTrimChoiceId_WhenChoiceIdContainsWhitespaces()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        // Act
        node.ChooseEventOption("  listen  ");

        // Assert
        node.ChosenEventOptionId.Should().Be("listen");
        node.HasChosenEventOption.Should().BeTrue();
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenNodeIsNotResolved()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        // Act
        var act = () => node.ChooseEventOption("listen");

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Only a resolved node can receive an event choice.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceIdIsEmpty()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        // Act
        var act = () => node.ChooseEventOption(string.Empty);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event choice id is required.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceIdIsWhitespace()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();

        // Act
        var act = () => node.ChooseEventOption("   ");

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Event choice id is required.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrowDomainException_WhenChoiceWasAlreadyResolved()
    {
        // Arrange
        var node = CreateNode(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        node.Select();
        node.Resolve();
        node.ChooseEventOption("listen");

        // Act
        var act = () => node.ChooseEventOption("leave");

        // Assert
        act.Should()
            .Throw<DomainException>()
            .WithMessage("Current event choice has already been resolved.");
    }

    private static Node CreateNode(
        NodeEventType eventType = NodeEventType.Combat,
        int riskLevel = 10,
        string rewardProfile = "test-reward",
        int nodeDepth = 0,
        NodeId? parentNodeId = null,
        bool isRoomBossNode = false,
        NodeState initialState = NodeState.Available)
    {
        return Node.Create(
            eventType,
            riskLevel,
            rewardProfile,
            nodeDepth,
            parentNodeId,
            isRoomBossNode,
            initialState);
    }
}