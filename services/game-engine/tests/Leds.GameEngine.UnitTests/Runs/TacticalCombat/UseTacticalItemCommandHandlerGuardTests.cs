using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.Resolution;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Time;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.TacticalItems;

public sealed class UseTacticalItemCommandHandlerGuardTests
{
    [Fact]
    public async Task Handle_ShouldRejectMissingRunAndRunWithoutTacticalCombat()
    {
        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);
        var handler = Handler(repository);
        await FluentActions.Awaiting(() => handler.Handle(Command(Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>();

        var run = TestGameEngineFactory.CreateRun();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        await FluentActions.Awaiting(() => handler.Handle(Command(run.Id.Value), default))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_ShouldRejectEnemyTurnAlreadyActedAndPostDeathRestriction()
    {
        var enemyTurnRun = RunWithCombat(enemyActsFirst: true);
        var enemyRepo = Repo(enemyTurnRun);
        await FluentActions.Awaiting(() => Handler(enemyRepo).Handle(Command(enemyTurnRun.Id.Value), default))
            .Should().ThrowAsync<ConflictException>();

        var actedRun = RunWithCombat();
        actedRun.RequireActiveTacticalCombat().MarkActiveCombatantActed();
        await FluentActions.Awaiting(() => Handler(Repo(actedRun)).Handle(Command(actedRun.Id.Value), default))
            .Should().ThrowAsync<ConflictException>();

        var restrictedRun = RunWithCombat(postDeathRestriction: true);
        restrictedRun.RequireActiveTacticalCombat().RegisterCombatantDefeated();
        await FluentActions.Awaiting(() => Handler(Repo(restrictedRun)).Handle(Command(restrictedRun.Id.Value), default))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ShouldRejectMissingAndNonCombatUsableItems()
    {
        var missingRun = RunWithCombat();
        await FluentActions.Awaiting(() => Handler(Repo(missingRun)).Handle(
                Command(missingRun.Id.Value, Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>();

        var unusableRun = RunWithCombat();
        var unusable = RunItem.Create("item.none", "Inerte", "Test", RunItemType.Consumable,
            default, 1, RunItemEffectType.None, 0);
        unusableRun.AddRunItem(unusable);
        await FluentActions.Awaiting(() => Handler(Repo(unusableRun)).Handle(
                Command(unusableRun.Id.Value, unusable.Id.Value), default))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ShouldRejectReviveWithoutDefeatedTargetAndOutOfRangeConsumable()
    {
        var reviveRun = RunWithCombat();
        var revive = RunItem.Create("item.revive", "Rappel", "Test", RunItemType.Consumable,
            default, 1, RunItemEffectType.RevivePercent, 25);
        reviveRun.AddRunItem(revive);
        await FluentActions.Awaiting(() => Handler(Repo(reviveRun)).Handle(
                new UseTacticalItemCommand(reviveRun.Id.Value, revive.Id.Value, 0, 0, null), default))
            .Should().ThrowAsync<ConflictException>();

        var rangeRun = RunWithCombat(width: 8, height: 3);
        var heal = RunItem.Create("item.heal", "Soin", "Test", RunItemType.Consumable,
            default, 1, RunItemEffectType.Heal, 10);
        rangeRun.AddRunItem(heal);
        await FluentActions.Awaiting(() => Handler(Repo(rangeRun)).Handle(
                new UseTacticalItemCommand(rangeRun.Id.Value, heal.Id.Value, 7, 2, null), default))
            .Should().ThrowAsync<ConflictException>();
    }

    private static UseTacticalItemCommand Command(Guid runId, Guid? itemId = null) =>
        new(runId, itemId ?? Guid.NewGuid(), 0, 0, null);

    private static UseTacticalItemCommandHandler Handler(Mock<IRunRepository> repository)
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        return new UseTacticalItemCommandHandler(
            repository.Object,
            new Mock<ICombatResolutionService>().Object,
            new Mock<IRewardOfferRepository>().Object,
            clock.Object,
            new Mock<ICombatSkillEffectResolver>().Object);
    }

    private static Mock<IRunRepository> Repo(Run run)
    {
        var repository = new Mock<IRunRepository>();
        repository.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        return repository;
    }

    private static Run RunWithCombat(
        bool enemyActsFirst = false,
        bool postDeathRestriction = false,
        int width = 5,
        int height = 3)
    {
        var run = TestGameEngineFactory.CreateRun();
        var battlefield = TacticalBattlefield.Rehydrate(
            width, height, new int[width * height],
            Enumerable.Repeat(true, width * height).ToArray(),
            Enumerable.Repeat(true, width * height).ToArray());
        var ally = Combatant.CreateAlly("player.self", "A-Porteur", "Hero", 100);
        var enemy = Combatant.CreateEnemy("enemy.test", "Z-Ennemi", "Enemy", 100,
            speed: enemyActsFirst ? 20 : 5);
        var combat = TacticalCombat.Create(
            CombatId.New(), run.Id, run.CurrentRoom.Id, run.CurrentRoom.Nodes.First().Id,
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(width - 1, height - 1))],
            DateTime.UtcNow,
            postDeathBasicAttackOnlyEnabled: postDeathRestriction,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
        run.StartTacticalCombat(combat);
        return run;
    }
}
