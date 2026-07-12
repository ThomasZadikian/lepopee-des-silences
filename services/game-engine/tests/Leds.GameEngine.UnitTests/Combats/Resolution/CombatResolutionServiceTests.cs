using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
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

    private static (Run Run, Combat Combat) CreateReadyCombat(
        NodeEventType eventType, bool journalEnabled = false)
    {
        Run run;
        if (journalEnabled)
        {
            var roomWithTargetNode = TestGameEngineFactory.CreateThresholdRoomWithTargetInitialNode(eventType);
            run = Run.StartNew(
                playerId: Guid.NewGuid(),
                seed: "seed-unit-test-journal",
                generatorVersion: "gen-test",
                markovMatrixVersion: "markov-test",
                initialRoom: roomWithTargetNode.Room,
                startedAt: DateTimeOffset.UtcNow,
                journalEnabled: true);
            run.ChooseNode(roomWithTargetNode.TargetNode.Id);
        }
        else
        {
            run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType).Run;
        }

        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, []);
        var combat = Combat.Create(
            CombatId.New(),
            run.Id,
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);

        run.StartCombat(combat);

        return (run, combat);
    }

    private static (Run Run, Combat Combat) CreateCompletedCombat(
        NodeEventType eventType, bool journalEnabled = false)
    {
        var (run, combat) = CreateReadyCombat(eventType, journalEnabled);
        combat.MarkCompleted();
        return (run, combat);
    }

    private static (Run Run, Combat Combat) CreateFailedCombat(
        NodeEventType eventType, bool journalEnabled = false)
    {
        var (run, combat) = CreateReadyCombat(eventType, journalEnabled);
        combat.MarkFailed();
        return (run, combat);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardStatPoint_WhenRoomBossDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.RoomBoss);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        var offer = await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        offer.Should().NotBeNull();
        gateway.Verify(g => g.AwardStatPointAsync(run.PlayerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardStatPoint_WhenFinalBossDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.FinalBoss);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardStatPointAsync(run.PlayerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldNotAwardStatPoint_WhenNonBossCombatDefeated()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

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
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        var offer = await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        offer.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAppendJournalEntry_OnVictory_WhenJournalEnabled()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat, journalEnabled: true);
        var service = new CombatResolutionService(
            CreateRewardOfferFactory(), Mock.Of<IPlayerProfileGateway>(), Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        run.JournalEntries.Should().ContainSingle(entry => entry.Text.Contains("Sentinel") && entry.Text.Contains("vaincu"));
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAppendJournalEntry_OnDefeat_WhenJournalEnabled()
    {
        var (run, combat) = CreateFailedCombat(NodeEventType.Combat, journalEnabled: true);
        var service = new CombatResolutionService(
            CreateRewardOfferFactory(), Mock.Of<IPlayerProfileGateway>(), Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        run.JournalEntries.Should().ContainSingle(entry => entry.Text.Contains("Sentinel") && entry.Text.Contains("survécu"));
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldNotAppendJournalEntry_WhenJournalDisabled()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat, journalEnabled: false);
        var service = new CombatResolutionService(
            CreateRewardOfferFactory(), Mock.Of<IPlayerProfileGateway>(), Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        run.JournalEntries.Should().BeEmpty();
    }
}
