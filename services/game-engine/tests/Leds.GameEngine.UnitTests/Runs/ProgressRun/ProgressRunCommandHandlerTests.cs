using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.ProgressRun;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
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

        var depthBeforeProgress = run.CurrentRoom.CurrentNodeDepth;

        var handler = CreateHandler(run);

        // Act
        var response = await handler.Handle(
            new ProgressRunCommand(run.Id.Value),
            CancellationToken.None);

        // Assert: next layer is unlocked
        response.Run.CurrentRoom.CurrentNodeDepth
            .Should().Be(depthBeforeProgress + 1,
                because: "ProgressRun should increment currentNodeDepth by 1.");

        response.Run.CurrentRoom.AvailableNodes
            .Should().NotBeEmpty(
                because: "Children of the resolved combat node should be available.");

        response.Run.CurrentRoom.AvailableNodes
            .Should().OnlyContain(n => n.Row == response.Run.CurrentRoom.CurrentNodeDepth,
                because: "Available nodes must be at the new currentNodeDepth.");

        response.Run.Status.Should().Be("Active");
    }

    // -----------------------------------------------------------------------
    // Guard: active combat
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProgressRun_ShouldThrow_WhenRunHasActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();
        var combat = CombatInstance.Create(new[]
        {
            CombatantSnapshot.Create("p", "Player", CombatantSide.Player,
                40, 12, 6, 10),
            CombatantSnapshot.Create("e", "Enemy", CombatantSide.Enemy,
                30, 8, 4, 6),
        });
        run.ChooseNode(run.CurrentRoom.AvailableNodes.First().Id);
        run.SetActiveCombat(combat.Id);

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
        var offer = new RewardOfferFactory().CreateCombatRewardOffer(
            RewardSource.Combat, riskLevel: 25);
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
