using FluentAssertions;
using Leds.GameEngine.Application.Combats.Targeting;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Moq;

namespace Leds.GameEngine.UnitTests.Combats.Actions;

public sealed class CombatSkillActionValidatorTests
{
    private readonly CombatantSkill _strikeSkill;
    private readonly CombatantSkill _healSkill;
    private readonly Combatant _ally;
    private readonly Combatant _enemy;
    private readonly Combat _activeCombat;
    private readonly Mock<ICombatTargetingRuleValidator> _targetingRuleValidator;
    private readonly CombatSkillActionValidator _validator;

    public CombatSkillActionValidatorTests()
    {
        _strikeSkill = CombatantSkill.Create(
            key: "skill.basic.strike",
            displayName: "Frappe",
            skillType: "Damage",
            targetingType: "SingleEnemy",
            effectType: "Damage",
            manaCost: 5,
            chargeCost: 0,
            basePower: 10);

        _healSkill = CombatantSkill.Create(
            key: "skill.basic.heal",
            displayName: "Soin",
            skillType: "Heal",
            targetingType: "Self",
            effectType: "Heal",
            manaCost: 3,
            chargeCost: 0,
            basePower: 15);

        _ally = Combatant.CreateAlly(
            sourceKey: "player.self",
            displayName: "Hero",
            archetype: "Fighter",
            maxVitality: 100,
            skills: [_strikeSkill, _healSkill]);

        _enemy = Combatant.CreateEnemy(
            sourceKey: "enemy.sentinel",
            displayName: "Sentinel",
            archetype: "Guard",
            maxVitality: 80,
            skills: [_strikeSkill]);

        _activeCombat = Combat.Create(
            id: CombatId.New(),
            runId: RunId.New(),
            roomId: RoomId.New(),
            nodeId: NodeId.New(),
            allies: [_ally],
            enemies: [_enemy]);

        _targetingRuleValidator = new Mock<ICombatTargetingRuleValidator>();
        _targetingRuleValidator
            .Setup(v => v.Validate(
                It.IsAny<Combat>(),
                It.IsAny<Combatant>(),
                It.IsAny<CombatantSkill>(),
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(new CombatTargetingValidationResult(true, null, [_enemy]));

        _validator = new CombatSkillActionValidator(_targetingRuleValidator.Object);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCombatIsNotActive()
    {
        var completedCombat = Combat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [_ally], [_enemy]);
        completedCombat.MarkCompleted();

        var result = _validator.Validate(
            completedCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public void Validate_ShouldFail_WhenActorDoesNotExist()
    {
        var result = _validator.Validate(
            _activeCombat, Guid.NewGuid(), "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not exist");
    }

    [Fact]
    public void Validate_ShouldFail_WhenActorIsDefeated()
    {
        _ally.MarkDefeated();

        var result = _validator.Validate(
            _activeCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("defeated");
    }

    [Fact]
    public void Validate_ShouldFail_WhenActorDoesNotOwnSkill()
    {
        var result = _validator.Validate(
            _activeCombat, _ally.Id.Value, "skill.unknown.missing", [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not own skill");
    }

    [Fact]
    public void Validate_ShouldDelegateTargetingRules()
    {
        _validator.Validate(_activeCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        _targetingRuleValidator.Verify(v => v.Validate(
            _activeCombat,
            _ally,
            _strikeSkill,
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == _enemy.Id.Value)), Times.Once);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTargetingRulesFail()
    {
        _targetingRuleValidator
            .Setup(v => v.Validate(_activeCombat, _ally, _strikeSkill, It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(new CombatTargetingValidationResult(false, "Targeting failed.", []));

        var result = _validator.Validate(
            _activeCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Targeting failed.");
    }

    [Fact]
    public void Validate_ShouldReturnResolvedTargets_WhenTargetingRulesSucceed()
    {
        _targetingRuleValidator
            .Setup(v => v.Validate(_activeCombat, _ally, _strikeSkill, It.IsAny<IReadOnlyCollection<Guid>>()))
            .Returns(new CombatTargetingValidationResult(true, null, [_enemy]));

        var result = _validator.Validate(
            _activeCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _enemy.Id);
    }

    [Fact]
    public void Validate_ShouldPreserveActorAndSkill_WhenValid()
    {
        var result = _validator.Validate(
            _activeCombat, _ally.Id.Value, "skill.basic.strike", [_enemy.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Actor.Should().Be(_ally);
        result.Skill.Should().Be(_strikeSkill);
    }
}