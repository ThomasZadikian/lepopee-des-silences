using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Combats.Actions;
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
    private static readonly CombatantSkill StrikeSkill = CombatantSkill.Create(
        key: "skill.basic.strike",
        displayName: "Frappe",
        skillType: "Damage",
        targetingType: "SingleEnemy",
        effectType: "Damage",
        manaCost: 5,
        chargeCost: 0,
        basePower: 10);

    private static readonly Combatant Ally = Combatant.CreateAlly(
        sourceKey: "player.self",
        displayName: "Hero",
        archetype: "Fighter",
        maxVitality: 100,
        skills: [StrikeSkill]);

    private static readonly Combatant Enemy = Combatant.CreateEnemy(
        sourceKey: "enemy.sentinel",
        displayName: "Sentinel",
        archetype: "Guard",
        maxVitality: 80,
        skills: []);

    private static Run CreateRunWithActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Combat);
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = Combat.Create(
            CombatId.New(),
            runWithNode.Run.Id,
            RoomId.New(),
            NodeId.New(),
            allies: [Ally],
            enemies: [Enemy]);
        runWithNode.Run.StartCombat(combat);
        return runWithNode.Run;
    }

    private static (Mock<IRunRepository> RunRepo, Mock<ICombatSkillActionValidator> Validator, Mock<IClock> Clock)
        CreateMocks(Run run, CombatSkillActionValidationResult validationResult)
    {
        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var validator = new Mock<ICombatSkillActionValidator>();
        validator
            .Setup(v => v.Validate(
                It.IsAny<Combat>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(validationResult);

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        return (runRepo, validator, clock);
    }

    [Fact]
    public async Task Handle_ShouldReturnResult_WhenActionIsValid()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: true,
            ErrorMessage: null,
            Actor: Ally,
            Skill: StrikeSkill,
            Targets: [Enemy]);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var result = await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Accepted.Should().BeTrue();
        result.Combat.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldLoadRun()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var (runRepo, validator, clock) = CreateMocks(run,
            new CombatSkillActionValidationResult(true, null, Ally, StrikeSkill, [Enemy]));

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        runRepo.Verify(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()), Times.Once);
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
            new Mock<IClock>().Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(
                RunId: Guid.NewGuid(),
                CombatId: Guid.NewGuid(),
                ActorId: Guid.NewGuid(),
                SkillKey: "skill.basic.strike",
                TargetIds: [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Run*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRunHasNoActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRun();

        var runRepo = new Mock<IRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object,
            new Mock<ICombatSkillActionValidator>().Object,
            new Mock<IClock>().Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: Guid.NewGuid(),
                ActorId: Guid.NewGuid(),
                SkillKey: "skill.basic.strike",
                TargetIds: [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no active combat*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCombatIdDoesNotMatchActiveCombat()
    {
        var run = CreateRunWithActiveCombat();
        var otherCombatId = CombatId.New();

        var (runRepo, validator, clock) = CreateMocks(run,
            new CombatSkillActionValidationResult(true, null, Ally, StrikeSkill, [Enemy]));

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: otherCombatId.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActorDoesNotOwnSkill()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: false,
            ErrorMessage: "Actor does not own skill 'skill.unknown'.",
            Actor: null,
            Skill: null,
            Targets: []);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var act = () => handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.unknown",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*does not own skill*");
    }

    [Fact]
    public async Task Handle_ShouldReturnCombatRuntimeDto()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: true, null, Ally, StrikeSkill, [Enemy]);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var result = await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        result.Combat.Id.Should().Be(combat.Id);
        result.Combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public async Task Handle_ShouldNotApplyDamageYet()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: true, null, Ally, StrikeSkill, [Enemy]);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var result = await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        result.Combat.Enemies.Single().CurrentVitality.Should().Be(80);
    }

    [Fact]
    public async Task Handle_ShouldNotCompleteCombatYet()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: true, null, Ally, StrikeSkill, [Enemy]);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var result = await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        result.Combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public async Task Handle_ShouldIncludeLogEntry_WhenActionIsAccepted()
    {
        var run = CreateRunWithActiveCombat();
        var combat = run.ActiveCombat!;

        var validationResult = new CombatSkillActionValidationResult(
            IsValid: true, null, Ally, StrikeSkill, [Enemy]);

        var (runRepo, validator, clock) = CreateMocks(run, validationResult);

        var handler = new UseCombatSkillCommandHandler(
            runRepo.Object, validator.Object, clock.Object);

        var result = await handler.Handle(
            new UseCombatSkillCommand(
                RunId: run.Id.Value,
                CombatId: combat.Id.Value,
                ActorId: Ally.Id.Value,
                SkillKey: "skill.basic.strike",
                TargetIds: [Enemy.Id.Value]),
            CancellationToken.None);

        result.LogEntries.Should().NotBeEmpty();
        result.LogEntries.Should().Contain(e => e.Type == "ActionAccepted");
        result.LogEntries.Should().Contain(e => e.ActorId == Ally.Id.Value);
        result.LogEntries.Should().Contain(e => e.SkillKey == "skill.basic.strike");
    }
}
