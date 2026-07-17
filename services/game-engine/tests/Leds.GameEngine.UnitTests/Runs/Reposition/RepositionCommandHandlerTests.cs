using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.Reposition;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Time;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.Reposition;

public sealed class RepositionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSwitchActorToBackRow()
    {
        var setup = CreateRunWithActiveCombat();
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        var result = await handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, "Back"),
            CancellationToken.None);

        setup.Ally.Row.Should().Be(CombatRow.Back);
        result.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSwitchActorBackToFrontRow()
    {
        var setup = CreateRunWithActiveCombat();
        setup.Ally.SetRow(CombatRow.Back);
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        await handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, "Front"),
            CancellationToken.None);

        setup.Ally.Row.Should().Be(CombatRow.Front);
    }

    [Fact]
    public async Task Handle_ShouldConsumeActorsTurn_AndAdvanceToNextCombatant()
    {
        var setup = CreateRunWithActiveCombat();
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        var result = await handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, "Back"),
            CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(setup.Enemy.Id.Value,
            because: "Reposition costs the actor's whole turn, like a basic attack.");
    }

    [Fact]
    public async Task Handle_ShouldReturnRepositionActionKey_AndSelfTarget()
    {
        var setup = CreateRunWithActiveCombat();
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        var result = await handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, "Back"),
            CancellationToken.None);

        result.SkillKey.Should().Be("action.reposition");
        result.TargetIds.Should().ContainSingle(id => id == setup.Ally.Id.Value);
        result.LogEntries.Should().Contain(e => e.Type == "RepositionDeclared");
    }

    [Fact]
    public async Task Handle_ShouldPersistCombatState_ViaHotPath()
    {
        var setup = CreateRunWithActiveCombat();
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        await handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, "Back"),
            CancellationToken.None);

        runRepo.Verify(r => r.UpdateActiveCombatStateAsync(setup.Run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRunDoesNotExist()
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Run?)null);
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, Mock.Of<ICombatResolutionService>());

        var act = () => handler.Handle(
            new RepositionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Back"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActorIsNotActiveCombatant()
    {
        var setup = CreateRunWithActiveCombat();
        var (runRepo, resolution, clock) = CreateMocks(setup.Run);
        var handler = new RepositionCommandHandler(runRepo.Object, clock.Object, resolution.Object);

        var act = () => handler.Handle(
            new RepositionCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Enemy.Id.Value, "Back"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("It is not this combatant's turn.");
    }

    private static (Mock<IRunRepository> RunRepo, Mock<ICombatResolutionService> Resolution, Mock<IClock> Clock)
        CreateMocks(Run run)
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var resolution = new Mock<ICombatResolutionService>();
        resolution.Setup(r => r.ApplyOutcomeAsync(
                It.IsAny<Run>(), It.IsAny<Combat>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RewardOffer?)null);

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        return (runRepo, resolution, clock);
    }

    private static (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) CreateRunWithActiveCombat()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(),
            runWithNode.Run.Id,
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);

        runWithNode.Run.StartCombat(combat);

        return (runWithNode.Run, combat, ally, enemy);
    }
}
