using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.SetRoomRiskTier;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.SetRoomRiskTier;

public sealed class SetRoomRiskTierCommandHandlerTests
{
    private static (Run Run, MapNode CombatNodeA, MapNode CombatNodeB) CreateRunWithTwoAvailableCombatNodes()
    {
        var combatNodeA = MapNode.Create(
            NodeEventType.Combat,
            riskLevel: 25,
            rewardProfile: "standard",
            row: 0,
            lane: 1,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available,
            combatRiskTier: RiskTier.Tendu);

        var combatNodeB = MapNode.Create(
            NodeEventType.Rare,
            riskLevel: 25,
            rewardProfile: "standard",
            row: 0,
            lane: 2,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available,
            combatRiskTier: RiskTier.Calme);

        var bossProfile = RoomBossProfile.Create(
            bossId: "threshold-guardian",
            name: "Gardien du Seuil",
            roomType: RoomType.Threshold,
            dangerHint: "High",
            enemyTemplateKey: "boss-threshold-guardian-v1");

        var bossNode = MapNode.Create(
            NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: 4,
            lane: 4,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: true,
            initialState: NodeState.Available,
            combatRiskTier: RiskTier.Perilleux);

        var fillerNodes = new[]
        {
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 0, lane: 3, []),
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 1, lane: 0, []),
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 2, lane: 0, []),
        };

        var room = Room.Create(
            depth: 0,
            roomType: RoomType.Threshold,
            theme: "Threshold",
            bossProfile: bossProfile,
            nodes: new[] { combatNodeA, combatNodeB, bossNode }.Concat(fillerNodes),
            gridWidth: 5,
            gridHeight: 5,
            movementBudget: 10,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "test-grid-v1",
            layoutTemplateVersion: "1.0.0");

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-room-risk-tier",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow);

        return (run, combatNodeA, combatNodeB);
    }

    [Fact]
    public async Task Handle_ShouldSetEveryAvailableCombatNode_ToTheChosenTier_AndPersistTheRun()
    {
        var (run, combatNodeA, combatNodeB) = CreateRunWithTwoAvailableCombatNodes();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new SetRoomRiskTierCommandHandler(repo.Object);

        var response = await handler.Handle(
            new SetRoomRiskTierCommand(run.Id.Value, RiskTier.Fatal),
            CancellationToken.None);

        combatNodeA.CombatRiskTier.Should().Be(RiskTier.Fatal);
        combatNodeB.CombatRiskTier.Should().Be(RiskTier.Fatal);
        response.Run.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLowerTheTier_UnlikeRaiseNodeRisk()
    {
        var (run, combatNodeA, _) = CreateRunWithTwoAvailableCombatNodes();
        combatNodeA.SetCombatRiskTier(RiskTier.Fatal);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new SetRoomRiskTierCommandHandler(repo.Object);

        await handler.Handle(
            new SetRoomRiskTierCommand(run.Id.Value, RiskTier.Calme),
            CancellationToken.None);

        combatNodeA.CombatRiskTier.Should().Be(RiskTier.Calme);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new SetRoomRiskTierCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new SetRoomRiskTierCommand(Guid.NewGuid(), RiskTier.Fatal),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
