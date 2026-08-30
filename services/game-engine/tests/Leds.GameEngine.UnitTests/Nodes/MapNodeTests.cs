using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Nodes;

public sealed class MapNodeTests
{
    // -----------------------------------------------------------------------
    // CombatRiskTier / IsCombatFlavored (risk redesign)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(NodeEventType.Combat, true)]
    [InlineData(NodeEventType.Rare, true)]
    [InlineData(NodeEventType.Elite, true)]
    [InlineData(NodeEventType.RoomBoss, true)]
    [InlineData(NodeEventType.FinalBoss, true)]
    [InlineData(NodeEventType.Item, false)]
    [InlineData(NodeEventType.Npc, false)]
    [InlineData(NodeEventType.Memory, false)]
    [InlineData(NodeEventType.Rest, false)]
    [InlineData(NodeEventType.Merchant, false)]
    [InlineData(NodeEventType.Law, false)]
    [InlineData(NodeEventType.Curse, false)]
    public void IsCombatFlavored_ShouldReturnExpectedResult(NodeEventType eventType, bool expected)
    {
        MapNode.IsCombatFlavored(eventType).Should().Be(expected);
    }

    [Fact]
    public void Create_ShouldAcceptACombatRiskTier_ForACombatFlavoredNode()
    {
        var node = MapNode.Create(
            NodeEventType.Elite, 60, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Dangereux);

        node.CombatRiskTier.Should().Be(RiskTier.Dangereux);
    }

    [Fact]
    public void Create_ShouldThrow_WhenANonCombatNodeHasACombatRiskTier()
    {
        var act = () => MapNode.Create(
            NodeEventType.Item, 60, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Dangereux);

        act.Should().Throw<DomainException>()
            .WithMessage("Non-combat node type 'Item' must not have a CombatRiskTier.");
    }

    [Fact]
    public void Create_ShouldLeaveCombatRiskTierNull_ByDefault()
    {
        var node = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>());

        node.CombatRiskTier.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // RaiseRisk ("provoquer le destin")
    // -----------------------------------------------------------------------

    [Fact]
    public void RaiseRisk_ShouldIncreaseTierByOneStep()
    {
        var node = MapNode.Create(
            NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Tendu);

        node.RaiseRisk();

        node.CombatRiskTier.Should().Be(RiskTier.Dangereux);
    }

    [Fact]
    public void RaiseRisk_ShouldBeRepeatable_UpToFatal()
    {
        var node = MapNode.Create(
            NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Calme);

        node.RaiseRisk();
        node.RaiseRisk();
        node.RaiseRisk();
        node.RaiseRisk();

        node.CombatRiskTier.Should().Be(RiskTier.Fatal);
    }

    [Fact]
    public void RaiseRisk_ShouldThrow_WhenAlreadyAtFatal()
    {
        var node = MapNode.Create(
            NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Fatal);

        var act = () => node.RaiseRisk();

        act.Should().Throw<DomainException>()
            .WithMessage("This node's risk is already at the maximum tier (Fatal).");
    }

    [Fact]
    public void RaiseRisk_ShouldThrow_WhenNodeIsNotCombatFlavored()
    {
        var node = MapNode.Create(NodeEventType.Item, 25, "standard", 0, 0, Array.Empty<NodeId>());

        var act = () => node.RaiseRisk();

        act.Should().Throw<DomainException>()
            .WithMessage("Only combat-flavored nodes can have their risk raised; 'Item' is not one.");
    }

    [Fact]
    public void RaiseRisk_ShouldThrow_WhenNodeHasNoCombatRiskTier()
    {
        var node = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>());

        var act = () => node.RaiseRisk();

        act.Should().Throw<DomainException>()
            .WithMessage("This combat node has no CombatRiskTier to raise.");
    }

    [Fact]
    public void RaiseRisk_ShouldThrow_WhenNodeIsNotAvailable()
    {
        var node = MapNode.Create(
            NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(),
            combatRiskTier: RiskTier.Tendu);
        node.Select();

        var act = () => node.RaiseRisk();

        act.Should().Throw<DomainException>()
            .WithMessage("Only an available MapNode's risk can be raised.");
    }

    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());

        node.Id.Value.Should().NotBeEmpty();
        node.EventType.Should().Be(NodeEventType.Combat);
        node.Row.Should().Be(0);
        node.Lane.Should().Be(0);
        node.RiskLevel.Should().Be(25);
        node.RewardProfile.Should().Be("standard");
        node.ParentNodeIds.Should().BeEmpty();
        node.IsBoss.Should().BeFalse();
        node.State.Should().Be(NodeState.Available);
        node.IsInitial.Should().BeTrue();
        node.IsAvailable.Should().BeTrue();
        node.IsPlanned.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldTrimRewardProfile()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "  standard  ",
            0,
            0,
            Array.Empty<NodeId>());

        node.RewardProfile.Should().Be("standard");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRiskLevelIsOutOfBounds()
    {
        var act1 = () => MapNode.Create(NodeEventType.Combat, -1, "standard", 0, 0, Array.Empty<NodeId>());
        var act2 = () => MapNode.Create(NodeEventType.Combat, 101, "standard", 0, 0, Array.Empty<NodeId>());

        act1.Should().Throw<DomainException>().WithMessage("MapNode risk level must be between 0 and 100.");
        act2.Should().Throw<DomainException>().WithMessage("MapNode risk level must be between 0 and 100.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRewardProfileIsNullOrWhitespace()
    {
        var act1 = () => MapNode.Create(NodeEventType.Combat, 25, "", 0, 0, Array.Empty<NodeId>());
        var act2 = () => MapNode.Create(NodeEventType.Combat, 25, "   ", 0, 0, Array.Empty<NodeId>());

        act1.Should().Throw<DomainException>().WithMessage("MapNode reward profile is required.");
        act2.Should().Throw<DomainException>().WithMessage("MapNode reward profile is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowIsNegative()
    {
        var act = () => MapNode.Create(NodeEventType.Combat, 25, "standard", -1, 0, Array.Empty<NodeId>());

        act.Should().Throw<DomainException>().WithMessage("MapNode row must be greater than or equal to 0.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenLaneIsNegative()
    {
        var act = () => MapNode.Create(NodeEventType.Combat, 25, "standard", 0, -1, Array.Empty<NodeId>());

        act.Should().Throw<DomainException>().WithMessage("MapNode lane must be greater than or equal to 0.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenInitialStateIsInvalid()
    {
        var act1 = () => MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(), initialState: NodeState.Selected);
        var act2 = () => MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, Array.Empty<NodeId>(), initialState: NodeState.Resolved);

        act1.Should().Throw<DomainException>().WithMessage("A newly created MapNode must be Planned or Available.");
        act2.Should().Throw<DomainException>().WithMessage("A newly created MapNode must be Planned or Available.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenParentNodeIdsIsNull()
    {
        var act = () => MapNode.Create(NodeEventType.Combat, 25, "standard", 1, 0, null!);

        act.Should().Throw<DomainException>().WithMessage("Parent node ids are required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowZeroHasParents()
    {
        var act = () => MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            new[] { NodeId.New() });

        act.Should().Throw<DomainException>().WithMessage("Initial row MapNodes cannot have parents.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenBossHasInvalidEventType()
    {
        var act = () => MapNode.Create(
            NodeEventType.Combat,
            50,
            "boss",
            2,
            0,
            Array.Empty<NodeId>(),
            isBoss: true);

        act.Should().Throw<DomainException>().WithMessage("A boss MapNode must have a RoomBoss or FinalBoss event type.");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenBossHasRoomBossEventType()
    {
        var act = () => MapNode.Create(
            NodeEventType.RoomBoss,
            85,
            "boss",
            2,
            0,
            new[] { NodeId.New() },
            isBoss: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_ShouldSucceed_WhenBossHasFinalBossEventType()
    {
        var act = () => MapNode.Create(
            NodeEventType.FinalBoss,
            100,
            "final-boss",
            5,
            0,
            new[] { NodeId.New() },
            isBoss: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_ShouldDeduplicateParentNodeIds()
    {
        var parentId = NodeId.New();
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { parentId, parentId, parentId });

        node.ParentNodeIds.Should().ContainSingle().Which.Should().Be(parentId);
    }

    [Fact]
    public void Create_ShouldSetIsInitialFalse_WhenRowIsNotZero()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { NodeId.New() });

        node.IsInitial.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetIsInitialFalse_WhenNodeIsBoss()
    {
        var node = MapNode.Create(
            NodeEventType.RoomBoss,
            85,
            "boss",
            2,
            0,
            new[] { NodeId.New() },
            isBoss: true);

        node.IsInitial.Should().BeFalse();
    }

    [Fact]
    public void Unlock_ShouldChangeStateToAvailable()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { NodeId.New() },
            initialState: NodeState.Planned);

        node.Unlock();

        node.State.Should().Be(NodeState.Available);
        node.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldThrow_WhenNodeIsNotPlanned()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>(),
            initialState: NodeState.Available);

        var act = () => node.Unlock();

        act.Should().Throw<DomainException>().WithMessage("Only a planned MapNode can be unlocked.");
    }

    [Fact]
    public void Select_ShouldChangeStateToSelected()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());

        node.Select();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Select_ShouldThrow_WhenNodeIsNotAvailable()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { NodeId.New() },
            initialState: NodeState.Planned);

        var act = () => node.Select();

        act.Should().Throw<DomainException>().WithMessage("Only an available MapNode can be selected.");
    }

    [Fact]
    public void Lock_ShouldChangeStateToLocked_WhenNodeIsAvailable()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            1,
            Array.Empty<NodeId>());

        node.Lock();

        node.State.Should().Be(NodeState.Locked);
    }

    [Fact]
    public void Lock_ShouldNotChangeState_WhenNodeIsSelected()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();

        node.Lock();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Lock_ShouldNotChangeState_WhenNodeIsResolved()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();
        node.Resolve();

        node.Lock();

        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void Lock_ShouldNotChangeState_WhenNodeIsPlanned()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { NodeId.New() },
            initialState: NodeState.Planned);

        node.Lock();

        node.State.Should().Be(NodeState.Planned);
    }

    [Fact]
    public void MarkUnreachable_ShouldChangeStateToUnreachable()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            1,
            Array.Empty<NodeId>());

        node.MarkUnreachable();

        node.State.Should().Be(NodeState.Unreachable);
    }

    [Fact]
    public void MarkUnreachable_ShouldNotChangeState_WhenNodeIsSelected()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();

        node.MarkUnreachable();

        node.State.Should().Be(NodeState.Selected);
    }

    [Fact]
    public void Resolve_ShouldChangeStateToResolved()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();

        node.Resolve();

        node.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenNodeIsNotSelected()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());

        var act = () => node.Resolve();

        act.Should().Throw<DomainException>().WithMessage("Only a selected MapNode can be resolved.");
    }

    [Fact]
    public void ChooseEventOption_ShouldSetChosenEventOptionId()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();
        node.Resolve();

        node.ChooseEventOption("option-1");

        node.ChosenEventOptionId.Should().Be("option-1");
        node.HasChosenEventOption.Should().BeTrue();
    }

    [Fact]
    public void ChooseEventOption_ShouldTrimChoiceId()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();
        node.Resolve();

        node.ChooseEventOption("  option-1  ");

        node.ChosenEventOptionId.Should().Be("option-1");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrow_WhenNodeIsNotResolved()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());

        var act = () => node.ChooseEventOption("option-1");

        act.Should().Throw<DomainException>().WithMessage("Only a resolved MapNode can receive an event choice.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrow_WhenChoiceIdIsNullOrWhitespace()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();
        node.Resolve();

        var act1 = () => node.ChooseEventOption("");
        var act2 = () => node.ChooseEventOption("   ");

        act1.Should().Throw<DomainException>().WithMessage("Event choice id is required.");
        act2.Should().Throw<DomainException>().WithMessage("Event choice id is required.");
    }

    [Fact]
    public void ChooseEventOption_ShouldThrow_WhenAlreadyHasChosenEventOption()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            0,
            0,
            Array.Empty<NodeId>());
        node.Select();
        node.Resolve();
        node.ChooseEventOption("option-1");

        var act = () => node.ChooseEventOption("option-2");

        act.Should().Throw<DomainException>().WithMessage("Current event choice has already been resolved.");
    }

    [Fact]
    public void AddParent_ShouldAddParentId()
    {
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            Array.Empty<NodeId>(),
            initialState: NodeState.Planned);
        var parentId = NodeId.New();

        node.AddParent(parentId);

        node.ParentNodeIds.Should().Contain(parentId);
    }

    [Fact]
    public void AddParent_ShouldNotDuplicateExistingParent()
    {
        var parentId = NodeId.New();
        var node = MapNode.Create(
            NodeEventType.Combat,
            25,
            "standard",
            1,
            0,
            new[] { parentId },
            initialState: NodeState.Planned);

        node.AddParent(parentId);

        node.ParentNodeIds.Should().ContainSingle().Which.Should().Be(parentId);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreNode_WithGivenValues()
    {
        var id = NodeId.New();
        var parentId = NodeId.New();

        var node = MapNode.Rehydrate(
            id,
            NodeEventType.Combat,
            1,
            0,
            30,
            "standard",
            new[] { parentId },
            false,
            NodeState.Resolved,
            "choice-1");

        node.Id.Should().Be(id);
        node.EventType.Should().Be(NodeEventType.Combat);
        node.Row.Should().Be(1);
        node.State.Should().Be(NodeState.Resolved);
        node.ChosenEventOptionId.Should().Be("choice-1");
        node.ParentNodeIds.Should().Contain(parentId);
    }
}
