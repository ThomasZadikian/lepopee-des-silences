using FluentAssertions;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Events.Resolvers;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events;

public sealed class NodeEventResolverTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static ResolvedNodeEventOutcomeDto ResolveDto(
        INodeEventResolver resolver,
        NodeEventType eventType)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType);

        var context = new NodeEventResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode);

        var result = resolver.Resolve(context);

        return ResolvedNodeEventOutcomeDto.FromResult(runWithNode.TargetNode, result);
    }

    // ---------------------------------------------------------------------------
    // Combat
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnCombatOutcome_WhenNodeTypeIsCombat()
    {
        var dto = ResolveDto(new CombatNodeEventResolver(), NodeEventType.Combat);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Combat.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.CombatStarted.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Elite
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnEliteOutcome_WhenNodeTypeIsElite()
    {
        var dto = ResolveDto(new EliteNodeEventResolver(), NodeEventType.Elite);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Elite.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.EliteEncounterStarted.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Item
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnItemOutcome_WhenNodeTypeIsItem()
    {
        var dto = ResolveDto(new ItemNodeEventResolver(), NodeEventType.Item);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Item.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.RewardGranted.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Rare
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnRareOutcome_WhenNodeTypeIsRare()
    {
        var dto = ResolveDto(new RareNodeEventResolver(), NodeEventType.Rare);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Rare.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.RareCombatStarted.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Non-regression — combat type isolation
    // ---------------------------------------------------------------------------

    [Fact]
    public void ItemNode_ShouldNotBeTreatedAsRareCombat()
    {
        var dto = ResolveDto(new ItemNodeEventResolver(), NodeEventType.Item);

        dto.ResolutionKind.Should().NotBe(NodeEventResolutionKind.RareCombatStarted.ToString(),
            because: "Item nodes grant rewards directly, they are not rare combats.");
    }

    [Fact]
    public void RareNode_ShouldNotReturnDirectItemReward()
    {
        var dto = ResolveDto(new RareNodeEventResolver(), NodeEventType.Rare);

        dto.ResolutionKind.Should().NotBe(NodeEventResolutionKind.RewardGranted.ToString(),
            because: "Rare is a combat encounter, not a direct reward node.");
    }

    [Fact]
    public void EliteNode_ShouldRemainCombatType()
    {
        var dto = ResolveDto(new EliteNodeEventResolver(), NodeEventType.Elite);

        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.EliteEncounterStarted.ToString(),
            because: "Elite nodes must remain a distinct combat tier from Rare.");
        dto.ResolutionKind.Should().NotBe(NodeEventResolutionKind.RareCombatStarted.ToString());
    }

    [Fact]
    public void RoomBossNode_ShouldRemainBossType()
    {
        var dto = ResolveDto(new RoomBossNodeEventResolver(), NodeEventType.RoomBoss);

        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.RoomBossEncounterStarted.ToString(),
            because: "RoomBoss nodes must keep their own resolution kind, distinct from Rare.");
        dto.ResolutionKind.Should().NotBe(NodeEventResolutionKind.RareCombatStarted.ToString());
    }

    // ---------------------------------------------------------------------------
    // Rest
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnRestOutcome_WhenNodeTypeIsRest()
    {
        var dto = ResolveDto(new RestNodeEventResolver(), NodeEventType.Rest);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Rest.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.RestResolved.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Merchant
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnMerchantOutcome_WhenNodeTypeIsMerchant()
    {
        var dto = ResolveDto(new MerchantNodeEventResolver(), NodeEventType.Merchant);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Merchant.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.TradeOffered.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse(
            because: "Merchant creates reward offer directly, no separate choice needed.");
        dto.Choices.Should().NotBeEmpty(
            because: "Merchant must offer at least one trade option.");
    }

    // ---------------------------------------------------------------------------
    // Npc
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnNpcOutcome_WhenNodeTypeIsNpc()
    {
        var dto = ResolveDto(new NpcNodeEventResolver(), NodeEventType.Npc);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Npc.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.NarrativeFragmentRevealed.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeTrue();
        dto.NarrativeFragments.Should().NotBeEmpty(
            because: "Npc interaction must include at least one narrative fragment.");
        dto.NarrativeFragments.Should().AllSatisfy(fragment =>
        {
            fragment.Speaker.Should().NotBeNullOrWhiteSpace();
            fragment.Text.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ---------------------------------------------------------------------------
    // RoomBoss
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnRoomBossOutcome_WhenNodeTypeIsRoomBoss()
    {
        // RoomBoss node type with isBoss=false is valid for resolver testing:
        // MapNode.Create only rejects (isBoss=true AND type not RoomBoss/FinalBoss).
        var dto = ResolveDto(new RoomBossNodeEventResolver(), NodeEventType.RoomBoss);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.RoomBoss.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.RoomBossEncounterStarted.ToString());
        dto.RiskLevel.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        dto.RewardProfile.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    [Fact]
    public void RoomBossResolver_ShouldIncludeBossNameInTitle_FromRoomBossProfile()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.RoomBoss);
        var bossProfileName = runWithNode.Run.CurrentRoom.BossProfile.Name;

        var context = new NodeEventResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode);

        var result = new RoomBossNodeEventResolver().Resolve(context);

        result.Title.Should().Be(bossProfileName);
        result.Description.Should().Contain(bossProfileName);
    }

    // ---------------------------------------------------------------------------
    // FinalBoss
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnFinalBossOutcome_WhenNodeTypeIsFinalBoss()
    {
        var dto = ResolveDto(new FinalBossNodeEventResolver(), NodeEventType.FinalBoss);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.FinalBoss.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.FinalBossEncounterStarted.ToString());
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Curse
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveCurrentEvent_ShouldReturnCurseOutcome_WhenNodeTypeIsCurse()
    {
        var dto = ResolveDto(new CurseNodeEventResolver(), NodeEventType.Curse);

        dto.NodeId.Should().NotBeEmpty();
        dto.PrimaryEventType.Should().Be(NodeEventType.Curse.ToString());
        dto.ResolutionKind.Should().Be(NodeEventResolutionKind.CurseOffered.ToString());
        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.RequiresPlayerChoice.Should().BeTrue();
        dto.Choices.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ---------------------------------------------------------------------------
    // Cross-type invariants
    // ---------------------------------------------------------------------------

    [Fact]
    public void AllResolvers_ShouldProduceDistinctResolutionKinds_ForCombatEliteAndRoomBoss()
    {
        var combatKind = new CombatNodeEventResolver()
            .Resolve(CreateContext(NodeEventType.Combat)).ResolutionKind;
        var eliteKind = new EliteNodeEventResolver()
            .Resolve(CreateContext(NodeEventType.Elite)).ResolutionKind;
        var roomBossKind = new RoomBossNodeEventResolver()
            .Resolve(CreateContext(NodeEventType.RoomBoss)).ResolutionKind;

        combatKind.Should().NotBe(eliteKind);
        eliteKind.Should().NotBe(roomBossKind);
        combatKind.Should().NotBe(roomBossKind);
    }

    [Fact]
    public void AllResolvers_ShouldRegisterCorrectEventType()
    {
        new CombatNodeEventResolver().EventType.Should().Be(NodeEventType.Combat);
        new EliteNodeEventResolver().EventType.Should().Be(NodeEventType.Elite);
        new ItemNodeEventResolver().EventType.Should().Be(NodeEventType.Item);
        new RareNodeEventResolver().EventType.Should().Be(NodeEventType.Rare);
        new RestNodeEventResolver().EventType.Should().Be(NodeEventType.Rest);
        new MerchantNodeEventResolver().EventType.Should().Be(NodeEventType.Merchant);
        new NpcNodeEventResolver().EventType.Should().Be(NodeEventType.Npc);
        new RoomBossNodeEventResolver().EventType.Should().Be(NodeEventType.RoomBoss);
        new FinalBossNodeEventResolver().EventType.Should().Be(NodeEventType.FinalBoss);
        new CurseNodeEventResolver().EventType.Should().Be(NodeEventType.Curse);
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static NodeEventResolutionContext CreateContext(NodeEventType eventType)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType);

        return new NodeEventResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode);
    }
}
