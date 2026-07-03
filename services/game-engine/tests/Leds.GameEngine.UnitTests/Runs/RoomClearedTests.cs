using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// PR 0.1.11 — RoomCleared transition state.
///
/// After a boss reward is selected:
/// - Run remains in <see cref="RunStatus.RoomResolved"/> (the "room cleared" state).
/// - No new room is generated.
/// - <see cref="Run.CurrentRoomIndex"/> is unchanged.
/// - <see cref="Run.HasPendingRewardOffer"/> is false.
/// - <see cref="ProgressRunCommandHandler"/> refuses to advance.
///
/// Non-boss reward selection leaves the run in <see cref="RunStatus.Active"/>.
/// </summary>
public sealed class RoomClearedTests
{
    // -----------------------------------------------------------------------
    // Setup helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a run in <see cref="RunStatus.RoomResolved"/> (boss defeated) with a
    /// <see cref="RewardSource.RoomBoss"/> reward offer pending.
    /// </summary>
    private static (Run run, RewardOffer offer) CreateRunWithBossRewardPending()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.RoomBoss,
            NodeEventType.RoomBoss,
            riskLevel: 85);

        run.SetPendingRewardOffer(offer.Id);

        return (run, offer);
    }

    // -----------------------------------------------------------------------
    // SelectBossReward — run state after boss reward selection
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectBossReward_ShouldSetRoomToRoomCleared()
    {
        var (run, offer) = CreateRunWithBossRewardPending();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Status.Should().Be(RunStatus.RoomResolved,
            because: "After boss reward selection the run must remain in RoomResolved — the cleared state awaiting the Interlude/MoveToNextRoom transition.");
    }

    [Fact]
    public async Task SelectBossReward_ShouldClearPendingReward()
    {
        var (run, offer) = CreateRunWithBossRewardPending();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.HasPendingRewardOffer.Should().BeFalse(
            because: "Boss reward selection must clear the pending reward offer.");
    }

    [Fact]
    public async Task SelectBossReward_ShouldNotGenerateNextRoom()
    {
        var (run, offer) = CreateRunWithBossRewardPending();
        var roomCountBefore = run.Rooms.Count;

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Rooms.Count.Should().Be(roomCountBefore,
            because: "SelectReward must not generate a new room — MoveToNextRoom is a future operation.");
    }

    [Fact]
    public async Task SelectBossReward_ShouldNotIncrementCurrentRoomIndex()
    {
        var (run, offer) = CreateRunWithBossRewardPending();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "CurrentRoomIndex must not change until MoveToNextRoom is called.");
    }

    [Fact]
    public async Task SelectBossReward_ShouldLeaveRunWaitingForTransition()
    {
        var (run, offer) = CreateRunWithBossRewardPending();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Status.Should().Be(RunStatus.RoomResolved,
            because: "After boss reward the run must remain in RoomResolved, awaiting Interlude or MoveToNextRoom.");
        run.HasPendingRewardOffer.Should().BeFalse();
        run.CurrentRoom.State.Should().Be(RoomState.Completed,
            because: "Room must stay Completed — boss was already defeated before reward selection.");
    }

    // -----------------------------------------------------------------------
    // ProgressRun guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldFail_WhenRoomIsCleared()
    {
        // RoomResolved + no pending reward = cleared room awaiting transition
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();

        var handler = new ProgressRunCommandHandler(runRepository.Object, choiceResolver.Object);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*room is cleared*");
    }

    // -----------------------------------------------------------------------
    // CompleteRoomBoss — domain-level: boss defeat + reward pending
    // -----------------------------------------------------------------------

    [Fact]
    public void CompleteRoomBoss_ShouldCreateBossRewardAndKeepRoomAwaitingReward()
    {
        // Simulate the post-boss-combat state that SubmitCombatActionCommandHandler produces
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.RoomBoss,
            NodeEventType.RoomBoss,
            riskLevel: 85);

        run.SetPendingRewardOffer(offer.Id);

        run.Status.Should().Be(RunStatus.RoomResolved,
            because: "Boss defeat sets run to RoomResolved.");
        run.HasPendingRewardOffer.Should().BeTrue(
            because: "Boss reward must be registered as pending before the player selects it.");
        run.CurrentRoom.State.Should().Be(RoomState.Completed,
            because: "Room must be fully Completed after the boss node is resolved.");
    }

    // -----------------------------------------------------------------------
    // SelectNonBossReward — run must remain Active
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectNonBossReward_ShouldKeepRunActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.Combat,
            NodeEventType.Combat,
            riskLevel: 25);

        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Active.ToString(),
            because: "Selecting a non-boss reward must leave the run Active for continued map progression.");
    }

    [Fact]
    public async Task SelectNonBossReward_ShouldNotSetRoomCleared()
    {
        var run = TestGameEngineFactory.CreateRun();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.Combat,
            NodeEventType.Combat,
            riskLevel: 25);

        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Status.Should().NotBe(RunStatus.RoomResolved,
            because: "Non-boss reward must not transition the run to RoomResolved.");
    }
}
