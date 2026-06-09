using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Combats.Targeting;

namespace Leds.GameEngine.UnitTests.Combats.Targeting;

public sealed class CombatTargetingRuleValidatorTests
{
    private readonly CombatTargetingRuleValidator _validator = new();
    private readonly CombatantSkill _selfSkill;
    private readonly CombatantSkill _singleEnemySkill;
    private readonly CombatantSkill _singleAllySkill;
    private readonly CombatantSkill _allEnemiesSkill;
    private readonly CombatantSkill _allAlliesSkill;
    private readonly CombatantSkill _unsupportedSkill;
    private readonly Combatant _ally;
    private readonly Combatant _ally2;
    private readonly Combatant _enemy;
    private readonly Combatant _enemy2;
    private readonly Combat _combat;

    public CombatTargetingRuleValidatorTests()
    {
        _selfSkill = CreateSkill("skill.basic.guard", "Self");
        _singleEnemySkill = CreateSkill("skill.basic.strike", "SingleEnemy");
        _singleAllySkill = CreateSkill("skill.basic.heal", "SingleAlly");
        _allEnemiesSkill = CreateSkill("skill.basic.cleave", "AllEnemies");
        _allAlliesSkill = CreateSkill("skill.basic.rally", "AllAllies");
        _unsupportedSkill = CreateSkill("skill.basic.unknown", "Unsupported");

        _ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, [_selfSkill, _singleEnemySkill]);
        _ally2 = Combatant.CreateAlly("player.ally2", "Sidekick", "Support", 60, [_singleAllySkill]);
        _enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80, [_singleEnemySkill]);
        _enemy2 = Combatant.CreateEnemy("enemy.skirmisher", "Skirmisher", "Skirmisher", 50, [_singleEnemySkill]);

        _combat = Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            [_ally, _ally2],
            [_enemy, _enemy2]);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenSelfTargetsActor()
    {
        var result = _validator.Validate(_combat, _ally, _selfSkill, [_ally.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _ally.Id);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSelfTargetsAnotherCombatant()
    {
        var result = _validator.Validate(_combat, _ally, _selfSkill, [_ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("themselves");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSelfHasNoTarget()
    {
        var result = _validator.Validate(_combat, _ally, _selfSkill, []);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exactly one target");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSelfHasMultipleTargets()
    {
        var result = _validator.Validate(_combat, _ally, _selfSkill, [_ally.Id.Value, _ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exactly one target");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllyTargetsEnemy()
    {
        var result = _validator.Validate(_combat, _ally, _singleEnemySkill, [_enemy.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _enemy.Id);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenEnemyTargetsAlly()
    {
        var result = _validator.Validate(_combat, _enemy, _singleEnemySkill, [_ally.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _ally.Id);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleEnemyTargetsAllyFromSameSide()
    {
        var result = _validator.Validate(_combat, _ally, _singleEnemySkill, [_ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("opposite side");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleEnemyHasMultipleTargets()
    {
        var result = _validator.Validate(_combat, _ally, _singleEnemySkill, [_enemy.Id.Value, _enemy2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exactly one target");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleEnemyTargetDoesNotExist()
    {
        var result = _validator.Validate(_combat, _ally, _singleEnemySkill, [Guid.NewGuid()]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not exist");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleEnemyTargetIsDefeated()
    {
        _enemy.MarkDefeated();

        var result = _validator.Validate(_combat, _ally, _singleEnemySkill, [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("defeated");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllyTargetsAlly()
    {
        var result = _validator.Validate(_combat, _ally, _singleAllySkill, [_ally2.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _ally2.Id);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenEnemyTargetsEnemy()
    {
        var result = _validator.Validate(_combat, _enemy, _singleAllySkill, [_enemy2.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().ContainSingle(t => t.Id == _enemy2.Id);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleAllyTargetsOppositeSide()
    {
        var result = _validator.Validate(_combat, _ally, _singleAllySkill, [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("same side");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSingleAllyHasMultipleTargets()
    {
        var result = _validator.Validate(_combat, _ally, _singleAllySkill, [_ally.Id.Value, _ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exactly one target");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllTargetsAreEnemies()
    {
        var result = _validator.Validate(_combat, _ally, _allEnemiesSkill, [_enemy.Id.Value, _enemy2.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllEnemiesContainsAlly()
    {
        var result = _validator.Validate(_combat, _ally, _allEnemiesSkill, [_enemy.Id.Value, _ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("opposite side");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllEnemiesContainsDefeatedTarget()
    {
        _enemy2.MarkDefeated();

        var result = _validator.Validate(_combat, _ally, _allEnemiesSkill, [_enemy.Id.Value, _enemy2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("defeated");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllEnemiesTargetsAreEmpty()
    {
        var result = _validator.Validate(_combat, _ally, _allEnemiesSkill, []);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("explicit targets");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllTargetsAreAllies()
    {
        var result = _validator.Validate(_combat, _ally, _allAlliesSkill, [_ally.Id.Value, _ally2.Id.Value]);

        result.IsValid.Should().BeTrue();
        result.Targets.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllAlliesContainsEnemy()
    {
        var result = _validator.Validate(_combat, _ally, _allAlliesSkill, [_ally.Id.Value, _enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("same side");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllAlliesContainsDefeatedTarget()
    {
        _ally2.MarkDefeated();

        var result = _validator.Validate(_combat, _ally, _allAlliesSkill, [_ally.Id.Value, _ally2.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("defeated");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAllAlliesTargetsAreEmpty()
    {
        var result = _validator.Validate(_combat, _ally, _allAlliesSkill, []);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("explicit targets");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTargetingTypeIsUnsupported()
    {
        var result = _validator.Validate(_combat, _ally, _unsupportedSkill, [_enemy.Id.Value]);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unsupported targeting type: Unsupported");
    }

    private static CombatantSkill CreateSkill(string key, string targetingType)
    {
        return CombatantSkill.Create(
            key: key,
            displayName: key,
            skillType: "Damage",
            targetingType: targetingType,
            effectType: "Damage",
            manaCost: 0,
            chargeCost: 0,
            basePower: 1);
    }
}
