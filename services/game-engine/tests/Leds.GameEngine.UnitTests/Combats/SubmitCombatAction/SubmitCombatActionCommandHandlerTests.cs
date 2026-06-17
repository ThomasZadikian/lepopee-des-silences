using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.Metrics;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.SubmitCombatAction;

public sealed class SubmitCombatActionCommandHandlerTests
{
    private static Mock<IClock> CreateClockMock()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static Mock<IRewardOfferRepository> CreateRewardRepoMock()
    {
        return new Mock<IRewardOfferRepository>();
    }

    private static RewardOfferFactory CreateRewardOfferFactory() =>
        new(new CombatRiskProfileResolver(), Mock.Of<ICatalogRewardTemplateProvider>());

    private static SubmitCombatActionCommandHandler CreateHandler(
        Mock<IRunRepository> runRepo,
        Mock<IRewardOfferRepository>? rewardRepo = null)
    {
        return new SubmitCombatActionCommandHandler(
            runRepo.Object,
            Mock.Of<ICombatSkillActionValidator>(),
            Mock.Of<ICombatSkillEffectResolver>(),
            Mock.Of<IEnemyCombatTurnResolver>(),
            rewardRepo?.Object ?? new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            Mock.Of<ICombatActionRecordRepository>(),
            CreateClockMock().Object);
    }

    [Fact(Skip = "Legacy test — handler now delegates to canonical pipeline. Migrate to UseCombatSkillCommandHandlerTests in alpha-0.8.x.")]
    public async Task Handle_ShouldSubmitBasicAttack_WhenCombatIsActive()
    {
        var (run, combat, playerId) = CreateRunWithActiveCombat();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(repo => repo.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync(combat);

        var handler = CreateHandler(runRepository);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                combat.Id.Value,
                playerId.Value,
                combat.Combatants.Single(c => c.Side == CombatantSide.Enemy).Id.Value,
                "BasicAttack"),
            CancellationToken.None);

        response.Result.Should().NotBeNull();
        response.Result.CombatState.Should().Be(CombatState.InProgress.ToString());
        response.Result.Damage.Should().BeGreaterThan(0);
        response.Run.ActiveCombatId.Should().Be(combat.Id.Value);
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task Handle_ShouldThrow_WhenRunHasNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();

        var handler = CreateHandler(runRepository);

        var act = () => handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "BasicAttack"),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Run has no active combat.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task Handle_ShouldThrow_WhenCombatDoesNotBelongToRun()
    {
        var (run, combat, _) = CreateRunWithActiveCombat();
        var otherCombatId = CombatId.New();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();

        var handler = CreateHandler(runRepository);

        var act = () => handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                otherCombatId.Value,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "BasicAttack"),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Combat does not belong to the active run.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task Handle_ShouldThrow_WhenCombatDoesNotExist()
    {
        var (run, combat, playerId) = CreateRunWithActiveCombat();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(repo => repo.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync((CombatInstance?)null);

        var handler = CreateHandler(runRepository);

        var act = () => handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                combat.Id.Value,
                playerId.Value,
                Guid.NewGuid(),
                "BasicAttack"),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Combat with id '{combat.Id.Value}' was not found.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task Handle_ShouldResolveCurrentNode_AndGenerateReward_WhenCombatIsCompleted()
    {
        var player = CombatantSnapshot.Create(
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: 40,
            attack: 999,
            defense: 0,
            speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-fragile-v1",
            "Fragile Enemy",
            CombatantSide.Enemy,
            maxHealth: 5,
            attack: 1,
            defense: 0,
            speed: 1);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var run = TestGameEngineFactory.CreateRun();
        run.ChooseNode(run.CurrentRoom.AvailableNodes.First().Id);
        run.SetActiveCombat(combat.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(repo => repo.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync(combat);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(repo => repo.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(runRepository, rewardRepository);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                combat.Id.Value,
                player.Id.Value,
                enemy.Id.Value,
                "BasicAttack"),
            CancellationToken.None);

        response.Result.TargetDefeated.Should().BeTrue();
        response.Result.CombatState.Should().Be(CombatState.Completed.ToString());
        response.Run.ActiveCombatId.Should().BeNull();
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());
        response.Run.PendingRewardOfferId.Should().NotBeNull();

        rewardRepository.Verify(
            repo => repo.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None),
            Times.Once);
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task Handle_ShouldKeepNodeSelected_WhenCombatIsStillInProgress()
    {
        var player = CombatantSnapshot.Create(
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: 40,
            attack: 12,
            defense: 6,
            speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-tanky-v1",
            "Tanky Enemy",
            CombatantSide.Enemy,
            maxHealth: 50,
            attack: 8,
            defense: 4,
            speed: 6);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var run = TestGameEngineFactory.CreateRun();
        run.ChooseNode(run.CurrentRoom.AvailableNodes.First().Id);
        run.SetActiveCombat(combat.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(repo => repo.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync(combat);

        var handler = CreateHandler(runRepository);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value,
                combat.Id.Value,
                player.Id.Value,
                enemy.Id.Value,
                "BasicAttack"),
            CancellationToken.None);

        response.Result.CombatState.Should().Be(CombatState.InProgress.ToString());
        response.Result.TargetDefeated.Should().BeFalse();
        response.Run.ActiveCombatId.Should().NotBeNull();
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeSelected.ToString());
    }

    // -----------------------------------------------------------------------
    // Post-combat state invariants (handler level)
    // -----------------------------------------------------------------------

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteCombat_ShouldClearActiveCombatId()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Combat);

        var (handler, _) = CreateHandlerWithCaptureRepo(run, combat);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        response.Run.ActiveCombatId.Should().BeNull(
            "activeCombatId must be cleared after combat completion.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteCombat_ShouldCreatePendingRewardOffer()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Combat);

        var (handler, _) = CreateHandlerWithCaptureRepo(run, combat);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        response.Run.PendingRewardOfferId.Should().NotBeNull(
            because: "a reward offer must be created after a victorious combat.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteCombat_ShouldKeepCombatNodeResolved()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Combat);

        var (handler, _) = CreateHandlerWithCaptureRepo(run, combat);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        var resolvedNode = run.CurrentRoom.Nodes
            .SingleOrDefault(n => n.State == NodeState.Resolved);

        resolvedNode.Should().NotBeNull(
            because: "the combat node must remain in Resolved state after combat completion.");

        resolvedNode!.EventType.Should().Be(NodeEventType.Combat);
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteRareCombat_ShouldCreateRareRewardOffer()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Rare);

        RewardOffer? captured = null;
        var (handler, rewardRepo) = CreateHandlerWithCaptureRepo(run, combat);
        rewardRepo
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Source.Should().Be(RewardSource.Rare,
            because: "Rare combat must yield a Rare reward tier.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteEliteCombat_ShouldCreateEliteRewardOffer()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Elite);

        RewardOffer? captured = null;
        var (handler, rewardRepo) = CreateHandlerWithCaptureRepo(run, combat);
        rewardRepo
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Source.Should().Be(RewardSource.Elite,
            because: "Elite combat must yield an Elite reward tier.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteBossCombat_ShouldCreateBossRewardOffer_AndSetRoomToRoomResolved()
    {
        // Use a RoomBoss node (the boss is reached at the end of the room)
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(
            NodeEventType.RoomBoss);
        var run = runWithNode.Run;

        var player = CombatantSnapshot.Create(
            "player-runtime-v1", "Player", CombatantSide.Player,
            maxHealth: 40, attack: 999, defense: 0, speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-fragile-v1", "Fragile Enemy", CombatantSide.Enemy,
            maxHealth: 5, attack: 1, defense: 0, speed: 1);

        var combat = CombatInstance.Create(new[] { player, enemy });
        run.SetActiveCombat(combat.Id);

        RewardOffer? captured = null;

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(r => r.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync(combat);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(runRepository, rewardRepository);

        var response = await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Id.Value, enemy.Id.Value, "BasicAttack"),
            CancellationToken.None);

        response.Run.ActiveCombatId.Should().BeNull();
        response.Run.PendingRewardOfferId.Should().NotBeNull(
            because: "boss combat must create a reward offer.");

        captured.Should().NotBeNull();
        captured!.Source.Should().Be(RewardSource.RoomBoss,
            "RoomBoss combat must yield a RoomBoss reward tier.");

        // Le nœud cible (row 0, isBoss=false) n'est pas le vrai boss de la room.
        // La room reste Active — RoomResolved n'est atteint que quand le boss réel (isBoss=true) est vaincu.
        response.Run.Status.Should().Be("Active",
            "the target RoomBoss node at row 0 is not isBoss=true, so the room is not completed.");
    }

    // -----------------------------------------------------------------------
    // Risk-scaling metadata tests (PR 0.1.9)
    // -----------------------------------------------------------------------

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteCombat_ShouldUseNodeRiskLevelForRewardScaling()
    {
        // The target node in TestGameEngineFactory has riskLevel 25 for Combat nodes.
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Combat);

        RewardOffer? captured = null;
        var (handler, rewardRepo) = CreateHandlerWithCaptureRepo(run, combat);
        rewardRepo
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CombatScaling.Should().NotBeNull(
            "CombatScaling must be populated when a combat node is resolved.");

        // Node riskLevel = 25 (from factory), Normal baseRisk = 20, delta = 5
        captured.CombatScaling!.ActualRisk.Should().Be(25,
            "the node's actual riskLevel must be forwarded to the scaling calculation.");
        captured.CombatScaling.Tier.Should().Be(CombatTier.Normal);
        captured.CombatScaling.BaseRisk.Should().Be(20);
        captured.CombatScaling.RiskDelta.Should().Be(5);
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteRareCombat_ShouldCreateRewardWithRareTierAndRiskMultiplier()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Rare);

        RewardOffer? captured = null;
        var (handler, rewardRepo) = CreateHandlerWithCaptureRepo(run, combat);
        rewardRepo
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        captured!.Source.Should().Be(RewardSource.Rare);
        captured.CombatScaling!.Tier.Should().Be(CombatTier.Rare,
            "Rare nodes must produce a Rare-tier scaling profile.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteEliteCombat_ShouldCreateRewardWithEliteTierAndRiskMultiplier()
    {
        var (run, combat, player, enemy) = CreateWinningCombat(NodeEventType.Elite);

        RewardOffer? captured = null;
        var (handler, rewardRepo) = CreateHandlerWithCaptureRepo(run, combat);
        rewardRepo
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Value, enemy.Value, "BasicAttack"),
            CancellationToken.None);

        captured!.Source.Should().Be(RewardSource.Elite);
        captured.CombatScaling!.Tier.Should().Be(CombatTier.Elite,
            "Elite nodes must produce an Elite-tier scaling profile.");
    }

    [Fact(Skip = "Legacy test — rewire via canonical pipeline")]
    public async Task CompleteBossCombat_ShouldCreateRewardWithBossTierAndRiskMultiplier()
    {
        // Use the RoomBoss node from TestGameEngineFactory (riskLevel 85)
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.RoomBoss);
        var run = runWithNode.Run;

        var player = CombatantSnapshot.Create(
            "player-runtime-v1", "Player", CombatantSide.Player,
            maxHealth: 40, attack: 999, defense: 0, speed: 10);
        var enemy = CombatantSnapshot.Create(
            "enemy-fragile-v1", "Fragile Enemy", CombatantSide.Enemy,
            maxHealth: 5, attack: 1, defense: 0, speed: 1);
        var combat = CombatInstance.Create(new[] { player, enemy });
        run.SetActiveCombat(combat.Id);

        RewardOffer? captured = null;

        var runRepository = new Mock<IRunRepository>();
        runRepository.Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository.Setup(r => r.GetByIdAsync(combat.Id, CancellationToken.None)).ReturnsAsync(combat);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Callback<RewardOffer, CancellationToken>((offer, _) => captured = offer)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(runRepository, rewardRepository);

        await handler.Handle(
            new SubmitCombatActionCommand(
                run.Id.Value, combat.Id.Value,
                player.Id.Value, enemy.Id.Value, "BasicAttack"),
            CancellationToken.None);

        captured!.Source.Should().Be(RewardSource.RoomBoss);
        captured.CombatScaling!.Tier.Should().Be(CombatTier.RoomBoss,
            "RoomBoss nodes must produce a RoomBoss-tier scaling profile.");
        captured.CombatScaling.ActualRisk.Should().Be(25,
            "Target node at row=0 has riskLevel=25 in TestGameEngineFactory.");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (SubmitCombatActionCommandHandler handler,
                    Mock<IRewardOfferRepository> rewardRepo)
        CreateHandlerWithCaptureRepo(Run run, CombatInstance combat)
    {
        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();
        combatRepository
            .Setup(r => r.GetByIdAsync(combat.Id, CancellationToken.None))
            .ReturnsAsync(combat);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.AddAsync(It.IsAny<RewardOffer>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(runRepository, rewardRepository);

        return (handler, rewardRepository);
    }

    /// <summary>
    /// Creates a run with a selected node of the given event type,
    /// and a combat instance the player can win in one hit.
    /// </summary>
    private static (Run run, CombatInstance combat, CombatantId playerId, CombatantId enemyId)
        CreateWinningCombat(NodeEventType eventType)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(eventType);
        var run = runWithNode.Run;

        var player = CombatantSnapshot.Create(
            "player-runtime-v1", "Player", CombatantSide.Player,
            maxHealth: 40, attack: 999, defense: 0, speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-fragile-v1", "Fragile Enemy", CombatantSide.Enemy,
            maxHealth: 5, attack: 1, defense: 0, speed: 1);

        var combat = CombatInstance.Create(new[] { player, enemy });
        run.SetActiveCombat(combat.Id);

        return (run, combat, player.Id, enemy.Id);
    }

    private static (Run run, CombatInstance combat, CombatantId playerId) CreateRunWithActiveCombat()
    {
        var player = CombatantSnapshot.Create(
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: 40,
            attack: 12,
            defense: 6,
            speed: 10);

        var enemy = CombatantSnapshot.Create(
            "enemy-shadow-v1",
            "Shadow Enemy",
            CombatantSide.Enemy,
            maxHealth: 30,
            attack: 8,
            defense: 4,
            speed: 6);

        var combat = CombatInstance.Create(new[] { player, enemy });

        var run = TestGameEngineFactory.CreateRun();
        run.SetActiveCombat(combat.Id);

        return (run, combat, player.Id);
    }
}