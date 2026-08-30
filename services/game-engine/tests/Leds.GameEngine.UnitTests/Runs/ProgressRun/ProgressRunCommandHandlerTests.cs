using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Rewards.Loot;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ProgressRun;

public sealed class ProgressRunCommandHandlerTests
{
    private static ProgressRunCommandHandler CreateHandler(Run run)
    {
        var runRepository = new Mock<IRunRepository>();
        runRepository
            .Setup(r => r.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        // For combat/non-choice nodes, RequiresChoice returns false.
        var choiceResolver = new Mock<ICurrentEventChoiceRequirementResolver>();
        choiceResolver
            .Setup(r => r.RequiresChoice(It.IsAny<MapNode>()))
            .Returns(false);

        return new ProgressRunCommandHandler(runRepository.Object, choiceResolver.Object);
    }

    // -----------------------------------------------------------------------
    // Core scenario: progression after resolved combat node
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldAllowProgressionAfterResolvedCombatNode()
    {
        // Arrange: run with combat node resolved (simulates state after combat victory)
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(
            NodeEventType.Combat);

        var run = runWithNode.Run;

        var handler = CreateHandler(run);

        // Act
        var response = await handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert: back to free exploration
        response.Run.CurrentRoom.State.Should().Be("Active");
        response.Run.CurrentRoom.AvailableNodes
            .Should().NotBeEmpty(
                because: "The other nodes revealed by fog of war remain available for exploration.");

        response.Run.Status.Should().Be("Active");
    }

    // -----------------------------------------------------------------------
    // Guard: active combat
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldThrow_WhenRunHasActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();
        TestGameEngineFactory.EnterNode(run, run.CurrentRoom.AvailableNodes.First());
        var ally = Combatant.CreateAlly("p", "Player", "Fighter", 40);
        var enemy = Combatant.CreateEnemy("e", "Enemy", "Guard", 30);
        var combat = TestTacticalCombatHelper.Create(run.Id, RoomId.New(), NodeId.New(), [ally], [enemy]);
        run.StartTacticalCombat(combat);

        var handler = CreateHandler(run);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Cannot progress while a combat is active.");
    }

    // -----------------------------------------------------------------------
    // Guard: pending reward
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldThrow_WhenPendingRewardExists()
    {
        var run = TestGameEngineFactory.CreateRun();
        var offer = new RewardOfferFactory(new CombatRiskProfileResolver(), Mock.Of<ICatalogContentGateway>(), new EnemyLootRewardBuilder(Mock.Of<ICatalogContentGateway>()))
            .CreateCombatRewardOffer(RewardSource.Combat, NodeEventType.Combat, riskLevel: (int)RiskTier.Tendu);
        run.SetPendingRewardOffer(offer.Id);

        var handler = CreateHandler(run);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Cannot progress while a pending reward offer requires selection.");
    }

    // -----------------------------------------------------------------------
    // Guard: no resolved node
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldThrow_WhenNoNodeIsResolved()
    {
        // Fresh run: no node resolved yet
        var run = TestGameEngineFactory.CreateRun();

        var handler = CreateHandler(run);

        var act = () => handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        // ProgressCurrentRoom → UnlockNextNodeLayer → throws because no resolved node
        await act.Should().ThrowAsync<DomainException>();
    }
}
