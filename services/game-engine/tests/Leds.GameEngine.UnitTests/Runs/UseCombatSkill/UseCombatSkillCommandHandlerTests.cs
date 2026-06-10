using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.UseCombatSkill;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Leds.GameEngine.Infrastructure.Combats.Targeting;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandHandlerTests
{
    private readonly CombatantSkill _strikeSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
    private readonly CombatantSkill _guardSkill = CreateSkill("skill.basic.guard", "Guard", "Self", 7);

    [Fact]
    public async Task Handle_ShouldReturnResult_WhenActionIsValid()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = ValidResult(setup.Ally, _strikeSkill, [setup.Enemy]);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.Combat.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldLoadRun()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = ValidResult(setup.Ally, _strikeSkill, [setup.Enemy]);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        runRepo.Verify(r => r.GetByIdAsync(setup.Run.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRunDoesNotExist()
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);
        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object,
            new Mock<ICombatSkillActionValidator>().Object,
            new Mock<ICombatSkillEffectResolver>().Object,
            new Mock<IEnemyCombatTurnResolver>().Object,
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            new Mock<IClock>().Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "skill.basic.strike", [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRunHasNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object,
            new Mock<ICombatSkillActionValidator>().Object,
            new Mock<ICombatSkillEffectResolver>().Object,
            new Mock<IEnemyCombatTurnResolver>().Object,
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            new Mock<IClock>().Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(run.Id.Value, Guid.NewGuid(), Guid.NewGuid(), "skill.basic.strike", [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*no active combat*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCombatIdDoesNotMatchActiveCombat()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = ValidResult(setup.Ally, _strikeSkill, [setup.Enemy]);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(setup.Run.Id.Value, Guid.NewGuid(), setup.Ally.Id.Value, _strikeSkill.Key, [setup.Enemy.Id.Value]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*does not match*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActorDoesNotOwnSkill()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = new CombatSkillActionValidationResult(false, "Actor does not own skill 'skill.unknown'.", null, null, []);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var act = () => handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*does not own skill*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTargetingIsInvalid()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = new CombatSkillActionValidationResult(
            false,
            "SingleEnemy targeting requires a target from the opposite side.",
            null,
            null,
            []);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var act = () => handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Ally]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*opposite side*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActorIsNotActiveCombatant()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = ValidResult(setup.Enemy, _strikeSkill, [setup.Ally]);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Enemy.Id.Value, _strikeSkill.Key, [setup.Ally.Id.Value]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("It is not this combatant's turn.");
    }

    [Fact]
    public async Task Handle_ShouldApplyDamage_WhenSkillEffectIsDamage()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Enemies.Single().CurrentVitality.Should().Be(70);
    }

    [Fact]
    public async Task Handle_ShouldIncreaseGuard_WhenSkillEffectIsGuard()
    {
        var setup = CreateRunWithActiveCombat(_guardSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _guardSkill, [setup.Ally], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _guardSkill, [setup.Ally]), CancellationToken.None);

        result.Combat.Allies.Single().Guard.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ShouldReturnUpdatedCombatRuntimeDto()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Id.Should().Be(setup.Combat.Id.Value);
        result.Combat.Enemies.Single().CurrentVitality.Should().Be(setup.Enemy.CurrentVitality);
    }

    [Fact]
    public async Task Handle_ShouldReturnLogEntries()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.LogEntries.Should().Contain(e => e.Type == "ActionAccepted");
        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Type == "DamageApplied");
    }

    [Fact]
    public async Task Handle_ShouldPersistUpdatedCombatOnRun()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out var runRepo, out _);

        await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        runRepo.Verify(r => r.UpdateAsync(setup.Run, It.IsAny<CancellationToken>()), Times.Once);
        setup.Run.ActiveCombat!.Enemies.Single().CurrentVitality.Should().Be(70);
    }

    [Fact]
    public async Task Handle_ShouldApplySkill_WhenActorIsActiveCombatant()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.Combat.Enemies.Single().CurrentVitality.Should().Be(70);
    }

    [Fact]
    public async Task Handle_ShouldAdvanceTurn_AfterSuccessfulAction()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(setup.Enemy.Id.Value);
        result.Combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldSkipDefeatedCombatants_WhenAdvancingTurn()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [lethalSkill]);
        var enemy1 = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, []);
        var enemy2 = Combatant.CreateEnemy("enemy.2", "Enemy2", "Guard", 80, []);
        var setup = CreateRunWithActiveCombat([ally], [enemy1, enemy2]);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, ally, [enemy1], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, ally, lethalSkill, [enemy1]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(enemy2.Id.Value);
        result.Combat.Enemies.Single(e => e.Id == enemy1.Id.Value).Status.Should().Be("Defeated");
    }

    [Fact]
    public async Task Handle_ShouldIncrementTurnNumber_WhenRoundCycles()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 1);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, []);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        setup.Combat.AdvanceTurn();
        var handler = CreateHandlerWithRealEffectResolver(setup, enemySkill, enemy, [ally], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, enemy, enemySkill, [ally]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(ally.Id.Value);
        result.Combat.TurnNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldNotTriggerEnemyTurnYet()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Allies.Single().CurrentVitality.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldMarkTargetDefeated_WhenDamageKillsTarget()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Enemies.Single().Status.Should().Be("Defeated");
        result.LogEntries.Should().Contain(e => e.Type == "TargetDefeated");
    }

    [Fact]
    public async Task Handle_ShouldCompleteCombat_WhenLastEnemyIsDefeated()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Status.Should().Be("Completed");
        result.Combat.ActiveCombatantId.Should().BeNull();
        result.CombatCompleted.Should().BeTrue();
        result.CombatFailed.Should().BeFalse();
        result.CanProgressRun.Should().BeTrue();
        result.RunStatus.Should().Be(RunStatus.Active.ToString());
        result.LogEntries.Should().Contain(e => e.Type == "CombatCompleted");
    }

    [Fact]
    public async Task Handle_ShouldClearActiveCombat_WhenCombatCompleted()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        setup.Run.ActiveCombat.Should().BeNull();
        setup.Run.HasActiveCombat.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFinalCombat_WhenCombatCompleted()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var finalCombatId = setup.Combat.Id;
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Id.Should().Be(finalCombatId.Value);
        result.Combat.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_ShouldSetCanProgressRun_WhenCombatCompleted()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.CanProgressRun.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFailCombat_WhenLastAllyIsDefeated()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 100);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, []);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [lethalSkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        setup.Combat.AdvanceTurn();
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, enemy, [ally], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, enemy, lethalSkill, [ally]), CancellationToken.None);

        result.Combat.Status.Should().Be("Failed");
        result.Combat.ActiveCombatantId.Should().BeNull();
        result.CombatFailed.Should().BeTrue();
        result.CombatCompleted.Should().BeFalse();
        result.CanProgressRun.Should().BeFalse();
        result.RunStatus.Should().Be(RunStatus.Failed.ToString());
        result.LogEntries.Should().Contain(e => e.Type == "CombatFailed");
    }

    [Fact]
    public async Task Handle_ShouldFailRun_WhenLastAllyIsDefeated()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 100);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, []);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [lethalSkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        setup.Combat.AdvanceTurn();
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, enemy, [ally], out _, out _);

        await handler.Handle(CreateCommand(setup, enemy, lethalSkill, [ally]), CancellationToken.None);

        setup.Run.Status.Should().Be(RunStatus.Failed);
        setup.Run.ActiveCombat.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnRunStatusFailed_WhenCombatFailed()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 100);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, []);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [lethalSkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        setup.Combat.AdvanceTurn();
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, enemy, [ally], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, enemy, lethalSkill, [ally]), CancellationToken.None);

        result.RunStatus.Should().Be(RunStatus.Failed.ToString());
    }

    [Fact]
    public async Task Handle_ShouldNotClearActiveCombat_WhenCombatStillActive()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        setup.Run.ActiveCombat.Should().BeSameAs(setup.Combat);
        setup.Run.HasActiveCombat.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldResolveCurrentEvent_WhenCombatCompleted()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        setup.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public async Task Handle_ShouldNotResolveCurrentEvent_WhenCombatFailed()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 100);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, []);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [lethalSkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        setup.Combat.AdvanceTurn();
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, enemy, [ally], out _, out _);

        await handler.Handle(CreateCommand(setup, enemy, lethalSkill, [ally]), CancellationToken.None);

        setup.Run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
    }

    [Fact]
    public async Task Handle_ShouldReturnUpdatedActiveCombatantId()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(setup.Enemy.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnTurnAdvancedLogEntry()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.LogEntries.Should().Contain(e => e.Type == "TurnAdvanced" && e.Message.Contains("Turn advanced"));
    }

    [Fact]
    public async Task Handle_ShouldNotAdvanceTurn_WhenCombatCompletes()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Status.Should().Be("Completed");
        result.Combat.ActiveCombatantId.Should().BeNull();
        result.LogEntries.Should().NotContain(e => e.Type == "TurnAdvanced");
    }

    [Fact]
    public async Task Handle_ShouldReturnResolvedTargetsInActionResult()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, setup.Ally, [setup.Enemy], out _, out _);

        var result = await handler.Handle(
            new UseCombatSkillCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, _strikeSkill.Key, [Guid.NewGuid()]),
            CancellationToken.None);

        result.TargetIds.Should().ContainSingle(id => id == setup.Enemy.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldResolveEnemyTurn_AfterPlayerAction_WhenNextCombatantIsEnemy()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_strikeSkill]);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally, _strikeSkill, [enemy], out _);

        var result = await handler.Handle(CreateCommand(setup, ally, _strikeSkill, [enemy]), CancellationToken.None);

        result.Combat.Allies.Single().CurrentVitality.Should().Be(90);
        result.Combat.ActiveCombatantId.Should().Be(ally.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldResolveMultipleEnemyTurns_UntilNextAlly()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_strikeSkill]);
        var enemy1 = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var enemy2 = Combatant.CreateEnemy("enemy.2", "Enemy2", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy1, enemy2]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally, _strikeSkill, [enemy1], out _);

        var result = await handler.Handle(CreateCommand(setup, ally, _strikeSkill, [enemy1]), CancellationToken.None);

        result.Combat.Allies.Single().CurrentVitality.Should().Be(80);
        result.Combat.ActiveCombatantId.Should().Be(ally.Id.Value);
        result.LogEntries.Count(e => e.Type == "EnemyTurnResolved").Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnUpdatedCombatAfterEnemyTurns()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_strikeSkill]);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally, _strikeSkill, [enemy], out _);

        var result = await handler.Handle(CreateCommand(setup, ally, _strikeSkill, [enemy]), CancellationToken.None);

        result.Combat.Allies.Single().CurrentVitality.Should().Be(setup.Run.ActiveCombat!.Allies.Single().CurrentVitality);
        result.Combat.Enemies.Single().CurrentVitality.Should().Be(setup.Run.ActiveCombat!.Enemies.Single().CurrentVitality);
    }

    [Fact]
    public async Task Handle_ShouldReturnEnemyActionLogs()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 10);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_strikeSkill]);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally, _strikeSkill, [enemy], out _);

        var result = await handler.Handle(CreateCommand(setup, ally, _strikeSkill, [enemy]), CancellationToken.None);

        result.LogEntries.Should().Contain(e => e.Type == "EnemyTurnResolved");
        result.LogEntries.Should().Contain(e => e.ActorId == enemy.Id.Value && e.Type == "SkillUsed");
    }

    [Fact]
    public async Task Handle_ShouldStopAutoEnemyTurns_WhenCombatCompletes()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEnemyResolver(setup, setup.Ally, lethalSkill, [setup.Enemy], out _);

        var result = await handler.Handle(CreateCommand(setup, setup.Ally, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Status.Should().Be("Completed");
        result.LogEntries.Should().NotContain(e => e.Type == "EnemyTurnResolved");
    }

    [Fact]
    public async Task Handle_ShouldStopAutoEnemyTurns_WhenCombatFails()
    {
        var enemySkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 107);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_guardSkill]);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [enemySkill]);
        var setup = CreateRunWithActiveCombat([ally], [enemy]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally, _guardSkill, [ally], out _);

        var result = await handler.Handle(CreateCommand(setup, ally, _guardSkill, [ally]), CancellationToken.None);

        result.Combat.Status.Should().Be("Failed");
        result.LogEntries.Should().Contain(e => e.Type == "CombatFailed");
    }

    [Fact]
    public async Task Handle_ShouldNotResolveEnemyTurn_WhenNextCombatantIsAlly()
    {
        var ally1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100, [_strikeSkill]);
        var ally2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100, [_strikeSkill]);
        var enemy = Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80, [_strikeSkill]);
        var setup = CreateRunWithActiveCombat([ally1, ally2], [enemy]);
        var handler = CreateHandlerWithRealEnemyResolver(setup, ally1, _strikeSkill, [enemy], out _);

        var result = await handler.Handle(CreateCommand(setup, ally1, _strikeSkill, [enemy]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(ally2.Id.Value);
        result.LogEntries.Should().NotContain(e => e.Type == "EnemyTurnResolved");
    }

    [Fact]
    public async Task Handle_ShouldPreventInfiniteEnemyTurnLoop()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = ValidResult(setup.Ally, _strikeSkill, [setup.Enemy]);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var enemyResolver = new Mock<IEnemyCombatTurnResolver>();
        enemyResolver.Setup(r => r.Resolve(setup.Combat))
            .Returns(new EnemyCombatTurnResolution(true, setup.Enemy.Id.Value, null, [], [], CombatRuntimeDto.FromDomain(setup.Combat)));
        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object,
            validator.Object,
            effectResolver.Object,
            enemyResolver.Object,
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            clock.Object);

        var act = () => handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Automatic enemy turn resolution exceeded the safety limit.");
    }

    private static UseCombatSkillCommandHandler CreateHandlerWithRealEffectResolver(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        out Mock<IRunRepository> runRepo,
        out Mock<ICombatSkillActionValidator> validator)
    {
        return CreateHandlerWithRealEffectResolver(setup, skill, setup.Ally, targets, out runRepo, out validator);
    }

    private static UseCombatSkillCommandHandler CreateHandlerWithRealEffectResolver(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        CombatantSkill skill,
        Combatant actor,
        IReadOnlyCollection<Combatant> targets,
        out Mock<IRunRepository> runRepo,
        out Mock<ICombatSkillActionValidator> validator)
    {
        var validationResult = ValidResult(actor, skill, targets);
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(setup.Run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Run);

        validator = new Mock<ICombatSkillActionValidator>();
        validator.Setup(v => v.Validate(setup.Combat, actor.Id.Value, skill.Key, It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(validationResult);

        return new UseCombatSkillCommandHandler(
            runRepo.Object,
            validator.Object,
            new CombatSkillEffectResolver(),
            CreateNoOpEnemyTurnResolver().Object,
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            clock.Object);
    }

    private static UseCombatSkillCommandHandler CreateHandlerWithRealEnemyResolver(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        out Mock<IRunRepository> runRepo)
    {
        var validationResult = ValidResult(actor, skill, targets);
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(setup.Run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Run);

        var validator = new Mock<ICombatSkillActionValidator>();
        validator.Setup(v => v.Validate(setup.Combat, actor.Id.Value, skill.Key, It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(validationResult);

        var realActionValidator = new CombatSkillActionValidator(new CombatTargetingRuleValidator());
        var effectResolver = new CombatSkillEffectResolver();

        return new UseCombatSkillCommandHandler(
            runRepo.Object,
            validator.Object,
            effectResolver,
            new EnemyCombatTurnResolver(realActionValidator, effectResolver),
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            clock.Object);
    }

    private static UseCombatSkillCommandHandler CreateHandler(
        Mock<IRunRepository> runRepo,
        Mock<ICombatSkillActionValidator> validator,
        Mock<ICombatSkillEffectResolver> effectResolver,
        Mock<IClock> clock)
    {
        return new UseCombatSkillCommandHandler(
            runRepo.Object,
            validator.Object,
            effectResolver.Object,
            CreateNoOpEnemyTurnResolver().Object,
            new Mock<IRewardOfferRepository>().Object,
            CreateRewardOfferFactory(),
            clock.Object);
    }

    private static RewardOfferFactory CreateRewardOfferFactory()
    {
        return new RewardOfferFactory(new CombatRiskProfileResolver());
    }

    private static Mock<IEnemyCombatTurnResolver> CreateNoOpEnemyTurnResolver()
    {
        var enemyTurnResolver = new Mock<IEnemyCombatTurnResolver>();
        enemyTurnResolver.Setup(r => r.Resolve(It.IsAny<Combat>()))
            .Returns((Combat combat) => new EnemyCombatTurnResolution(
                WasResolved: false,
                ActorId: combat.ActiveCombatantId?.Value,
                SkillKey: null,
                TargetIds: [],
                LogEntries: [],
                Combat: CombatRuntimeDto.FromDomain(combat)));

        return enemyTurnResolver;
    }

    private static (Mock<IRunRepository> RunRepo, Mock<ICombatSkillActionValidator> Validator, Mock<ICombatSkillEffectResolver> EffectResolver, Mock<IClock> Clock)
        CreateMocks(Run run, CombatSkillActionValidationResult validationResult)
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var validator = new Mock<ICombatSkillActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<Combat>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(validationResult);

        var effectResolver = new Mock<ICombatSkillEffectResolver>();
        effectResolver.Setup(r => r.Resolve(It.IsAny<Combat>(), It.IsAny<Combatant>(), It.IsAny<CombatantSkill>(), It.IsAny<IReadOnlyCollection<Combatant>>()))
            .Returns((Combat combat, Combatant actor, CombatantSkill skill, IReadOnlyCollection<Combatant> targets) =>
                new CombatSkillEffectResolution(true, [], combat));

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        return (runRepo, validator, effectResolver, clock);
    }

    private static CombatSkillActionValidationResult ValidResult(
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        return new CombatSkillActionValidationResult(true, null, actor, skill, targets);
    }

    private static UseCombatSkillCommand CreateCommand(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        return CreateCommand(setup, setup.Ally, skill, targets);
    }

    private static UseCombatSkillCommand CreateCommand(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        Combatant actor,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets)
    {
        return new UseCombatSkillCommand(
            setup.Run.Id.Value,
            setup.Combat.Id.Value,
            actor.Id.Value,
            skill.Key,
            targets.Select(t => t.Id.Value).ToArray());
    }

    private static (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) CreateRunWithActiveCombat(CombatantSkill allySkill)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [allySkill]);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, []);
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

    private static (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) CreateRunWithActiveCombat(
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = Combat.Create(
            CombatId.New(),
            runWithNode.Run.Id,
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);

        runWithNode.Run.StartCombat(combat);

        return (runWithNode.Run, combat, allies.First(), enemies.First());
    }

    private static CombatantSkill CreateSkill(string key, string effectType, string targetingType, int basePower)
    {
        return CombatantSkill.Create(
            key: key,
            displayName: key,
            skillType: effectType,
            targetingType: targetingType,
            effectType: effectType,
            manaCost: 0,
            chargeCost: 0,
            basePower: basePower);
    }
}
