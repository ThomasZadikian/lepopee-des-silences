using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Runs.GetCurrentCombat;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetCurrentCombat;

public sealed class GetCurrentCombatQueryHandlerTests
{
    private static Combat CreateActiveCombat(RunId runId)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        return Combat.Create(
            CombatId.New(),
            runId,
            RoomId.New(),
            NodeId.New(),
            new[] { ally },
            new[] { enemy });
    }

    [Fact]
    public async Task Handle_ShouldReturnCombat_WhenRunHasActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();
        var combat = CreateActiveCombat(run.Id);
        run.StartCombat(combat);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var result = await handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(combat.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var act = () => handler.Handle(
            new GetCurrentCombatQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenRunHasNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var act = () => handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*No active combat was found*");
    }

    [Fact]
    public async Task Handle_ShouldReturnCombatWithAllies()
    {
        var run = TestGameEngineFactory.CreateRun();
        var combat = CreateActiveCombat(run.Id);
        run.StartCombat(combat);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var result = await handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        result.Allies.Should().HaveCount(1);
        result.Allies.Single().SourceKey.Should().Be("player.self");
    }

    [Fact]
    public async Task Handle_ShouldReturnCombatWithEnemies()
    {
        var run = TestGameEngineFactory.CreateRun();
        var combat = CreateActiveCombat(run.Id);
        run.StartCombat(combat);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var result = await handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        result.Enemies.Should().HaveCount(1);
        result.Enemies.Single().SourceKey.Should().Be("enemy.sentinel");
    }

    [Fact]
    public async Task Handle_ShouldNotCallCombatFactory()
    {
        var run = TestGameEngineFactory.CreateRun();
        var combat = CreateActiveCombat(run.Id);
        run.StartCombat(combat);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var result = await handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        result.Id.Should().Be(combat.Id);
        result.Allies.Single().CurrentVitality.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_AfterCombatCompletedAndCleared()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var run = runWithNode.Run;
        var combat = CreateActiveCombat(run.Id);
        run.StartCombat(combat);
        combat.MarkCompleted();
        run.CompleteActiveCombat();

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new GetCurrentCombatQueryHandler(repo.Object);

        var act = () => handler.Handle(
            new GetCurrentCombatQuery(run.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*No active combat was found*");
    }
}
