using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.SubmitCombatAction;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
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

    private static RewardOfferFactory CreateRewardOfferFactory()
    {
        return new RewardOfferFactory();
    }

    [Fact]
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

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            CreateRewardRepoMock().Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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

    [Fact]
    public async Task Handle_ShouldThrow_WhenRunHasNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            CreateRewardRepoMock().Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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

    [Fact]
    public async Task Handle_ShouldThrow_WhenCombatDoesNotBelongToRun()
    {
        var (run, combat, _) = CreateRunWithActiveCombat();
        var otherCombatId = CombatId.New();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var combatRepository = new Mock<ICombatInstanceRepository>();

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            CreateRewardRepoMock().Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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

    [Fact]
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

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            CreateRewardRepoMock().Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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

    [Fact]
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

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            rewardRepository.Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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

    [Fact]
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

        var handler = new SubmitCombatActionCommandHandler(
            runRepository.Object,
            combatRepository.Object,
            CreateRewardRepoMock().Object,
            CreateRewardOfferFactory(),
            CreateClockMock().Object);

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
