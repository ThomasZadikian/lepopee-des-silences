using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Runs.MoveToNextRoom;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.MoveToNextRoom;

public sealed class MoveToNextRoomCommandHandlerTests
{
    /// <summary>
    /// Advances a fresh run through 9 rooms (defeating each boss) so it sits at depth 9 —
    /// the room right before crossing the first floor boundary (<c>FloorLengthInRooms</c> is
    /// 10). The *OnFloorEnd tests below need a transition that actually crosses a boundary,
    /// and <c>MoveToNextRoom</c> only ever accepts <c>CurrentDepth + 1</c> — so reaching
    /// depth 9 for real is the only way to make depth 10 both valid and floor-crossing.
    /// </summary>
    private static Run CreateRunAtFloorEnd()
    {
        var run = TestGameEngineFactory.CreateRun();

        for (var i = 0; i < 9; i++)
        {
            var bossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);
            TestGameEngineFactory.EnterNode(run, bossNode);
            run.ResolveCurrentEvent();

            run.EnterInterlude();
            run.MoveToNextRoom(TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1));
        }

        var finalBossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);
        TestGameEngineFactory.EnterNode(run, finalBossNode);
        run.ResolveCurrentEvent();

        return run;
    }

    [Fact]
    public async Task Handle_ShouldMoveRunToNextRoom_WhenCurrentRoomIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.EnterInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator
            .Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None))
            .ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object,
            generator.Object,
            palaceLawPromulgator.Object,
            playerProfileGateway);

        var response = await handler.Handle(
            new MoveToNextRoomCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentDepth.Should().Be(nextRoom.Depth);
        response.Run.CurrentRoom.Id.Should().Be(nextRoom.Id.Value);

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var generator = new Mock<IRunGenerator>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object,
            generator.Object,
            palaceLawPromulgator.Object,
            playerProfileGateway);

        var act = () => handler.Handle(
            new MoveToNextRoomCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    // "Loi de l'Oubli Partiel": Run.MoveToNextRoom signals (via its
    // FloorEndModifierConsumptionResult return) that the floor-scoped forgotten-skill
    // modifier was just consumed — the handler must pay out the +8 stat points then.
    [Fact]
    public async Task Handle_ShouldAwardStatPoints_WhenOubliPartielPayoutIsDueOnFloorEnd()
    {
        var run = CreateRunAtFloorEnd();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.SkillForgotten,
            1,
            RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law.oubli-partiel"));
        run.EnterInterlude();

        // CreateRunAtFloorEnd() leaves the run at depth 9, so a next room at depth 10
        // is both a valid +1 step and crosses exactly one floor boundary.
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 10);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator
            .Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None))
            .ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object,
            generator.Object,
            palaceLawPromulgator.Object,
            playerProfileGateway);

        await handler.Handle(new MoveToNextRoomCommand(run.Id.Value), CancellationToken.None);

        playerProfileGateway.AwardedStatPoints.Should()
            .ContainSingle(award => award.PlayerId == run.PlayerId && award.Amount == Run.SkillForgottenFloorEndStatPoints);
    }

    // "Loi de l'Impôt du Seuil": a toll is charged at the entry of every room while
    // the law is active — successful payment should not apply the insolvency debuff.
    [Fact]
    public async Task Handle_ShouldChargeTheRoomToll_AndNotApplyInsolvencyDebuff_WhenAffordable()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.RoomTollAmount, 5, RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw", sourceKey: "law.impot-seuil"));
        run.EnterInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 100);

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        await handler.Handle(new MoveToNextRoomCommand(run.Id.Value), CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should()
            .ContainSingle(attempt => attempt.PlayerId == run.PlayerId && attempt.Amount == 5 && attempt.Succeeded);
        run.RunModifiers.Should().NotContain(m => m.Type == RunModifierType.MaxHpReductionPercent);
    }

    [Fact]
    public async Task Handle_ShouldApplyInsolvencyDebuff_WhenTheRoomTollCannotBeAfforded()
    {
        var run = TestGameEngineFactory.CreateRunWithCompletedCurrentRoom();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.RoomTollAmount, 5, RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw", sourceKey: "law.impot-seuil"));
        run.EnterInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: run.CurrentDepth + 1);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 0);

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        await handler.Handle(new MoveToNextRoomCommand(run.Id.Value), CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should()
            .ContainSingle(attempt => attempt.PlayerId == run.PlayerId && attempt.Amount == 5 && !attempt.Succeeded);
        run.RunModifiers.Should().ContainSingle(m =>
            m.Type == RunModifierType.MaxHpReductionPercent
            && m.Value == Run.RoomTollInsolvencyMaxHpReductionPercent);
    }

    // "Loi du Prêteur": at floor end, the Palais claws back a fraction of the current
    // currency total.
    [Fact]
    public async Task Handle_ShouldClawBackAFractionOfCurrency_WhenPreteurClawbackIsDueOnFloorEnd()
    {
        var run = CreateRunAtFloorEnd();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.CurrencyGainBonusPercent, 50, RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw", sourceKey: "law.preteur"));
        run.EnterInterlude();

        // CreateRunAtFloorEnd() leaves the run at depth 9, so a next room at depth 10
        // is both a valid +1 step and crosses exactly one floor boundary.
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 10);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedCurrencyBalance(run.PlayerId, 100);

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        await handler.Handle(new MoveToNextRoomCommand(run.Id.Value), CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should()
            .ContainSingle(attempt => attempt.PlayerId == run.PlayerId && attempt.Amount == 25 && attempt.Succeeded);
    }

    [Fact]
    public async Task Handle_ShouldNotAttemptAClawback_WhenPreteurBalanceIsZero()
    {
        var run = CreateRunAtFloorEnd();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.CurrencyGainBonusPercent, 50, RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw", sourceKey: "law.preteur"));
        run.EnterInterlude();
        var nextRoom = TestGameEngineFactory.CreateThresholdRoom(depth: 10);

        var repository = new Mock<IRunRepository>();
        repository.Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None)).ReturnsAsync(run);

        var generator = new Mock<IRunGenerator>();
        generator.Setup(service => service.GenerateNextRoomAsync(run, CancellationToken.None)).ReturnsAsync(nextRoom);

        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        var playerProfileGateway = new StubPlayerProfileGateway();

        var handler = new MoveToNextRoomCommandHandler(
            repository.Object, generator.Object, palaceLawPromulgator.Object, playerProfileGateway);

        await handler.Handle(new MoveToNextRoomCommand(run.Id.Value), CancellationToken.None);

        playerProfileGateway.SpentCurrencyAttempts.Should().BeEmpty();
    }
}