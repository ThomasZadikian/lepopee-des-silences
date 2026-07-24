using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.RaiseNodeRisk;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.RaiseNodeRisk;

public sealed class RaiseNodeRiskCommandHandlerTests
{
    private static (Run Run, MapNode CombatNode) CreateRunWithAvailableCombatNode(
        RiskTier combatRiskTier = RiskTier.Tendu)
    {
        var combatNode = MapNode.Create(
            NodeEventType.Combat,
            riskLevel: 25,
            rewardProfile: "standard",
            row: 0,
            lane: 1,
            parentNodeIds: Array.Empty<NodeId>(),
            isBoss: false,
            initialState: NodeState.Available,
            combatRiskTier: combatRiskTier);

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
            initialState: NodeState.Available);

        var fillerNodes = new[]
        {
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 0, lane: 2, []),
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 0, lane: 3, []),
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 1, lane: 0, []),
            MapNode.Create(NodeEventType.Item, 10, "standard", row: 2, lane: 0, [])
        };

        var room = Room.Create(
            depth: 0,
            roomType: RoomType.Threshold,
            theme: "Threshold",
            bossProfile: bossProfile,
            nodes: new[] { combatNode, bossNode }.Concat(fillerNodes),
            gridWidth: 5,
            gridHeight: 5,
            movementBudget: 10,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "test-grid-v1",
            layoutTemplateVersion: "1.0.0");

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-wager",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow);

        return (run, combatNode);
    }

    [Fact]
    public async Task Handle_ShouldRaiseTheNodeRisk_AndPersistTheRun()
    {
        var (run, combatNode) = CreateRunWithAvailableCombatNode(RiskTier.Tendu);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new RaiseNodeRiskCommandHandler(repo.Object);

        var response = await handler.Handle(
            new RaiseNodeRiskCommand(run.Id.Value, combatNode.Id.Value),
            CancellationToken.None);

        combatNode.CombatRiskTier.Should().Be(RiskTier.Dangereux);
        response.Run.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new RaiseNodeRiskCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new RaiseNodeRiskCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenNodeIsAlreadyAtFatalTier()
    {
        var (run, combatNode) = CreateRunWithAvailableCombatNode(RiskTier.Fatal);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new RaiseNodeRiskCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new RaiseNodeRiskCommand(run.Id.Value, combatNode.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenNodeDoesNotBelongToTheCurrentRoom()
    {
        var (run, _) = CreateRunWithAvailableCombatNode();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var handler = new RaiseNodeRiskCommandHandler(repo.Object);

        var act = () => handler.Handle(
            new RaiseNodeRiskCommand(run.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
