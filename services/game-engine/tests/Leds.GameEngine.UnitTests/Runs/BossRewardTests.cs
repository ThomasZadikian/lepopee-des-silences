using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Rewards.SelectReward;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Domain.Combats;
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
/// The room boss no longer gates room progression (see <see cref="Run.ConfirmRoomExit"/>) —
/// resolving it, and selecting its reward, behaves exactly like any other node/reward:
/// the run stays <see cref="RunStatus.Active"/> throughout, and <see cref="ProgressRunCommandHandler"/>
/// returns the room to free exploration once the pending reward is cleared. There is no more
/// "room cleared, awaiting Interlude/MoveToNextRoom" transitional status.
/// </summary>
public sealed class BossRewardTests
{
    // -----------------------------------------------------------------------
    // Setup helpers
    // -----------------------------------------------------------------------

    /// <summary>Boss node resolved, with its <see cref="RewardSource.RoomBoss"/> reward
    /// offer pending selection.</summary>
    private static (Run run, RewardOffer offer) CreateRunWithBossRewardPending()
    {
        var run = TestGameEngineFactory.CreateRun();
        var bossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);
        TestGameEngineFactory.EnterNode(run, bossNode);
        run.ResolveCurrentEvent();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.RoomBoss,
            NodeEventType.RoomBoss,
            riskLevel: (int)RiskTier.Fatal);

        run.SetPendingRewardOffer(offer.Id);

        return (run, offer);
    }

    // -----------------------------------------------------------------------
    // SelectBossReward — run state after boss reward selection
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectBossReward_ShouldKeepRunActive()
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

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Status.Should().Be(RunStatus.Active,
            because: "the boss no longer gates the run's status — see Run.ConfirmRoomExit.");
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

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
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

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.Rooms.Count.Should().Be(roomCountBefore,
            because: "SelectReward must not generate a new room — ConfirmRoomExit is a separate operation.");
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

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.CurrentRoomIndex.Should().Be(0,
            because: "CurrentRoomIndex must not change until ConfirmRoomExit is called.");
    }

    [Fact]
    public async Task SelectBossReward_ShouldLeaveRoomNodeResolved_UntilProgressed()
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

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        var choiceId = offer.Choices.First().Id;

        await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        run.HasPendingRewardOffer.Should().BeFalse();
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved,
            because: "the boss node was resolved but ProgressRun hasn't returned the room to exploration yet.");
    }

    // -----------------------------------------------------------------------
    // ProgressRun — now succeeds once the boss reward is cleared, same as any other node
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldSucceed_AfterBossRewardIsCleared()
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

        var selectHandler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        await selectHandler.Handle(
            new SelectRewardCommand(run.Id.Value, offer.Choices.First().Id.Value),
            CancellationToken.None);

        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();
        var progressHandler = new ProgressRunCommandHandler(runRepository.Object, choiceResolver.Object);

        await progressHandler.Handle(new ProgressRunCommand(run.Id.Value), CancellationToken.None);

        run.CurrentRoom.State.Should().Be(RoomState.Active,
            because: "ProgressRun returns the room to free exploration — the boss no longer blocks this.");
    }

    // -----------------------------------------------------------------------
    // Boss node resolution — domain-level: boss defeat + reward pending
    // -----------------------------------------------------------------------

    [Fact]
    public void ResolvingBossNode_ShouldNotForceRunOrRoomIntoATerminalState()
    {
        // Simulate the post-boss-combat state that SubmitCombatActionCommandHandler produces
        var run = TestGameEngineFactory.CreateRun();
        var bossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);
        TestGameEngineFactory.EnterNode(run, bossNode);
        run.ResolveCurrentEvent();

        var factory = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()));
        var offer = factory.CreateCombatRewardOffer(
            RewardSource.RoomBoss,
            NodeEventType.RoomBoss,
            riskLevel: (int)RiskTier.Fatal);

        run.SetPendingRewardOffer(offer.Id);

        run.Status.Should().Be(RunStatus.Active,
            because: "Boss defeat no longer sets a special run status.");
        run.HasPendingRewardOffer.Should().BeTrue(
            because: "Boss reward must be registered as pending before the player selects it.");
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved,
            because: "the boss node resolves like any other — no RoomState.Completed anymore.");
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
            riskLevel: (int)RiskTier.Tendu);

        run.SetPendingRewardOffer(offer.Id);

        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var rewardRepository = new Mock<IRewardOfferRepository>();
        rewardRepository
            .Setup(r => r.GetByIdAsync(offer.Id, CancellationToken.None))
            .ReturnsAsync(offer);

        var handler = new SelectRewardCommandHandler(runRepository.Object, rewardRepository.Object, Mock.Of<ICatalogContentGateway>(), Mock.Of<IPlayerProfileGateway>());
        var choiceId = offer.Choices.First().Id;

        var response = await handler.Handle(
            new SelectRewardCommand(run.Id.Value, choiceId.Value),
            CancellationToken.None);

        response.Run.Status.Should().Be(RunStatus.Active.ToString(),
            because: "Selecting a non-boss reward must leave the run Active for continued map progression.");
    }
}
