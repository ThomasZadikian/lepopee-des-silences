using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.UseCombatSkill;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
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
            new Mock<IClock>().Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(run.Id.Value, Guid.NewGuid(), Guid.NewGuid(), "skill.basic.strike", [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no active combat*");
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

        await act.Should().ThrowAsync<DomainException>().WithMessage("*does not match*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActorDoesNotOwnSkill()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var validationResult = new CombatSkillActionValidationResult(false, "Actor does not own skill 'skill.unknown'.", null, null, []);
        var (runRepo, validator, effectResolver, clock) = CreateMocks(setup.Run, validationResult);
        var handler = CreateHandler(runRepo, validator, effectResolver, clock);

        var act = () => handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*does not own skill*");
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

        await act.Should().ThrowAsync<DomainException>().WithMessage("*opposite side*");
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

        result.Combat.Id.Should().Be(setup.Combat.Id);
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
    public async Task Handle_ShouldNotAdvanceTurnYet()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var activeCombatantId = setup.Combat.ActiveCombatantId;
        var turnNumber = setup.Combat.TurnNumber;
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.ActiveCombatantId.Should().Be(activeCombatantId);
        result.Combat.TurnNumber.Should().Be(turnNumber);
    }

    [Fact]
    public async Task Handle_ShouldNotTriggerEnemyTurnYet()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, _strikeSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Allies.Single().CurrentVitality.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldMarkTargetDefeated_WhenDamageKillsTarget()
    {
        var lethalSkill = CreateSkill("skill.basic.strike", "Damage", "SingleEnemy", 80);
        var setup = CreateRunWithActiveCombat(lethalSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, lethalSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(CreateCommand(setup, lethalSkill, [setup.Enemy]), CancellationToken.None);

        result.Combat.Enemies.Single().Status.Should().Be(CombatantStatus.Defeated);
        result.LogEntries.Should().Contain(e => e.Type == "TargetDefeated");
    }

    [Fact]
    public async Task Handle_ShouldReturnResolvedTargetsInActionResult()
    {
        var setup = CreateRunWithActiveCombat(_strikeSkill);
        var handler = CreateHandlerWithRealEffectResolver(setup, _strikeSkill, [setup.Enemy], out _, out _);

        var result = await handler.Handle(
            new UseCombatSkillCommand(setup.Run.Id.Value, setup.Combat.Id.Value, setup.Ally.Id.Value, _strikeSkill.Key, [Guid.NewGuid()]),
            CancellationToken.None);

        result.TargetIds.Should().ContainSingle(id => id == setup.Enemy.Id.Value);
    }

    private static UseCombatSkillCommandHandler CreateHandlerWithRealEffectResolver(
        (Run Run, Combat Combat, Combatant Ally, Combatant Enemy) setup,
        CombatantSkill skill,
        IReadOnlyCollection<Combatant> targets,
        out Mock<IRunRepository> runRepo,
        out Mock<ICombatSkillActionValidator> validator)
    {
        var validationResult = ValidResult(setup.Ally, skill, targets);
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(setup.Run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Run);

        validator = new Mock<ICombatSkillActionValidator>();
        validator.Setup(v => v.Validate(setup.Combat, setup.Ally.Id.Value, skill.Key, It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(validationResult);

        return new UseCombatSkillCommandHandler(
            runRepo.Object,
            validator.Object,
            new CombatSkillEffectResolver(),
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
            clock.Object);
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
        return new UseCombatSkillCommand(
            setup.Run.Id.Value,
            setup.Combat.Id.Value,
            setup.Ally.Id.Value,
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
