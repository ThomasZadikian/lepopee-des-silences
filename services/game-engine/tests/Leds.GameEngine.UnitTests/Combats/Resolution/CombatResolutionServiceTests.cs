using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.Resolution;

public sealed class CombatResolutionServiceTests
{
    private static RewardOfferFactory CreateRewardOfferFactory()
    {
        return new RewardOfferFactory(
            new CombatRiskProfileResolver(),
            Mock.Of<ICatalogContentGateway>(),
            new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
    }

    private static (Run Run, Combat Combat) CreateCompletedCombat(NodeEventType eventType)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, []);
        var combat = Combat.Create(
            CombatId.New(),
            runWithNode.Run.Id,
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);

        runWithNode.Run.StartCombat(combat);
        combat.MarkCompleted();

        return (runWithNode.Run, combat);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardStatPoint_WhenRoomBossDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.RoomBoss);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<ILogger<CombatResolutionService>>());

        var offer = await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        offer.Should().NotBeNull();
        gateway.Verify(g => g.AwardStatPointAsync(run.PlayerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardStatPoint_WhenFinalBossDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.FinalBoss);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardStatPointAsync(run.PlayerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldNotAwardStatPoint_WhenNonBossCombatDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardStatPointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldStillReturnRewardOffer_WhenGatewayThrows()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.RoomBoss);
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway.Setup(g => g.AwardStatPointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Player Service unreachable"));
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<ILogger<CombatResolutionService>>());

        var offer = await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        offer.Should().NotBeNull();
    }
}
