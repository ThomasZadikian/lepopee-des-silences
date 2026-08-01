using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
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

    private static (Run Run, TacticalCombat Combat) CreateReadyCombat(
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
            TestGameEngineFactory.EnterNode(run, roomWithTargetNode.TargetNode);
        }
        else
        {
            run = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType).Run;
        }

        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, []);
        var combat = TestTacticalCombatHelper.Create(run.Id, RoomId.New(), NodeId.New(), [ally], [enemy]);

        run.StartTacticalCombat(combat);

        return (run, combat);
    }

    private static (Run Run, TacticalCombat Combat) CreateCompletedCombat(
        NodeEventType eventType, bool journalEnabled = false)
    {
        var (run, combat) = CreateReadyCombat(eventType, journalEnabled);
        var enemy = combat.Enemies.Single();
        enemy.ApplyVitalityDamage(enemy.CurrentVitality);
        combat.CompleteIfAllEnemiesDefeated();
        return (run, combat);
    }

    private static (Run Run, TacticalCombat Combat) CreateFailedCombat(
        NodeEventType eventType, bool journalEnabled = false)
    {
        var (run, combat) = CreateReadyCombat(eventType, journalEnabled);
        var ally = combat.Allies.Single();
        ally.ApplyVitalityDamage(ally.CurrentVitality);
        combat.FailIfAllAlliesDefeated();
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
    public async Task ApplyOutcomeAsync_ShouldAwardCombatEclats_WhenCombatCompletes()
    {
        // Test fixtures never set an explicit CombatRiskTier, so the node falls back to
        // RiskTier.Tendu (see CombatResolutionService.CreateRewardOfferAsync), which grants
        // 1 Éclat (see CombatRiskProfileResolver.EclatsBaseAmountByTier).
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardCurrencyAsync(run.PlayerId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldStillReturnRewardOffer_WhenEclatsGatewayThrows()
    {
        var (run, combat) = CreateCompletedCombat(NodeEventType.Combat);
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway.Setup(g => g.AwardCurrencyAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Player Service unreachable"));
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        var offer = await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        offer.Should().NotBeNull();
    }

    /// <summary>
    /// Builds a ready-to-resolve combat whose triggering node has an explicit
    /// <see cref="RiskTier"/> — the default test fixtures leave CombatRiskTier null
    /// (falls back to Tendu in CombatResolutionService), so "Éclats de Him'Lit" tests
    /// need a node built with a real tier instead.
    /// </summary>
    private static (Run Run, TacticalCombat Combat) CreateReadyCombatWithRiskTier(RiskTier riskTier, int enemyCount = 1)
    {
        var roomWithTargetNode = TestGameEngineFactory.CreateThresholdRoomWithTargetInitialNode(
            NodeEventType.Combat, targetCombatRiskTier: riskTier);

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-him-lit",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: roomWithTargetNode.Room,
            startedAt: DateTimeOffset.UtcNow);

        TestGameEngineFactory.EnterNode(run, roomWithTargetNode.TargetNode);

        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemies = Enumerable.Range(0, enemyCount)
            .Select(i => Combatant.CreateEnemy($"enemy.sentinel.{i}", "Sentinel", "Guard", 80, []))
            .ToArray();
        var combat = TestTacticalCombatHelper.Create(run.Id, RoomId.New(), NodeId.New(), [ally], enemies);

        run.StartTacticalCombat(combat);

        return (run, combat);
    }

    private static (Run Run, TacticalCombat Combat) CreateCompletedCombatWithRiskTier(RiskTier riskTier, int enemyCount = 1)
    {
        var (run, combat) = CreateReadyCombatWithRiskTier(riskTier, enemyCount);
        foreach (var enemy in combat.Enemies)
        {
            enemy.ApplyVitalityDamage(enemy.CurrentVitality);
        }
        combat.CompleteIfAllEnemiesDefeated();
        return (run, combat);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardTwoHimLitShardsPerEnemy_WhenCombatIsFatalTier()
    {
        var (run, combat) = CreateCompletedCombatWithRiskTier(RiskTier.Fatal, enemyCount: 3);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardHimLitCurrencyAsync(run.PlayerId, 6, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldAwardOneHimLitShardPerEnemy_WhenCombatIsPerilleuxTier()
    {
        var (run, combat) = CreateCompletedCombatWithRiskTier(RiskTier.Perilleux, enemyCount: 2);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardHimLitCurrencyAsync(run.PlayerId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldNotAwardHimLitShards_WhenCombatIsBelowPerilleuxTier()
    {
        var (run, combat) = CreateCompletedCombatWithRiskTier(RiskTier.Dangereux);
        var gateway = new Mock<IPlayerProfileGateway>();
        var service = new CombatResolutionService(CreateRewardOfferFactory(), gateway.Object, Mock.Of<IOutboxWriter>(), Mock.Of<ILogger<CombatResolutionService>>());

        await service.ApplyOutcomeAsync(run, combat, DateTimeOffset.UtcNow);

        gateway.Verify(g => g.AwardHimLitCurrencyAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyOutcomeAsync_ShouldStillReturnRewardOffer_WhenHimLitGatewayThrows()
    {
        var (run, combat) = CreateCompletedCombatWithRiskTier(RiskTier.Fatal);
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway.Setup(g => g.AwardHimLitCurrencyAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
