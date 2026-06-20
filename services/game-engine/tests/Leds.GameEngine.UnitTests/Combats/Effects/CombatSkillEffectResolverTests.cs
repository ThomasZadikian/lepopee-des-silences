using FluentAssertions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.Effects;

public sealed class CombatSkillEffectResolverTests
{
    private readonly CombatSkillEffectResolver _resolver = new();

    [Fact]
    public void Resolve_ShouldApplyDamage_WhenEffectTypeIsDamage()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.Applied.Should().BeTrue();
        enemy.CurrentVitality.Should().Be(70);
    }

    [Fact]
    public void Resolve_ShouldApplyDamage_WhenEffectTypeIsDamageVitality()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "DamageVitality", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.Applied.Should().BeTrue();
        enemy.CurrentVitality.Should().Be(70);
    }

    [Fact]
    public void Resolve_ShouldConsumeGuardBeforeVitality_WhenDamageIsApplied()
    {
        var (combat, ally, enemy) = CreateCombat();
        enemy.GainGuard(5);
        var skill = CreateSkill("skill.basic.strike", "Damage", 12);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.Guard.Should().Be(0);
        enemy.CurrentVitality.Should().Be(73);
    }

    [Fact]
    public void Resolve_ShouldMarkTargetDefeated_WhenDamageReducesVitalityToZero()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 80);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.Status.Should().Be(CombatantStatus.Defeated);
    }

    [Fact]
    public void Resolve_ShouldCreateDamageLogEntries()
    {
        var (combat, ally, enemy) = CreateCombat();
        enemy.GainGuard(3);
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().Contain(e => e.Type == "SkillUsed");
        result.LogEntries.Should().Contain(e => e.Message.Contains("guard absorbs 3 damage"));
        result.LogEntries.Should().Contain(e => e.Type == "DamageApplied");
    }

    [Fact]
    public void Resolve_ShouldIncreaseGuard_WhenEffectTypeIsGuard()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.guard", "Guard", 8);

        _resolver.Resolve(combat, ally, skill, [ally]);

        ally.Guard.Should().Be(8);
    }

    [Fact]
    public void Resolve_ShouldCreateGuardLogEntry()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.guard", "Guard", 8);

        var result = _resolver.Resolve(combat, ally, skill, [ally]);

        result.LogEntries.Should().ContainSingle(e => e.Type == "GuardGained");
        result.LogEntries.Single().Message.Should().Contain("gains 8 guard");
    }

    [Fact]
    public void Resolve_ShouldCreateWeakenLogEntry_WhenEffectTypeIsWeaken()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.weaken", "Weaken", 0);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().ContainSingle(e => e.Message.Contains("weakens"));
    }

    [Fact]
    public void Resolve_ShouldNotModifyVitality_WhenEffectTypeIsWeaken()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.weaken", "Weaken", 0);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldCreateDisruptLogEntry_WhenEffectTypeIsDisrupt()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.disrupt", "Disrupt", 0);

        var result = _resolver.Resolve(combat, ally, skill, [enemy]);

        result.LogEntries.Should().ContainSingle(e => e.Message.Contains("disrupts"));
    }

    [Fact]
    public void Resolve_ShouldNotModifyVitality_WhenEffectTypeIsDisrupt()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.disrupt", "Disrupt", 0);

        _resolver.Resolve(combat, ally, skill, [enemy]);

        enemy.CurrentVitality.Should().Be(80);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenEffectTypeIsUnsupported()
    {
        var (combat, ally, enemy) = CreateCombat();
        var skill = CreateSkill("skill.unknown", "Unknown", 0);

        var act = () => _resolver.Resolve(combat, ally, skill, [enemy]);

        act.Should().Throw<DomainException>().WithMessage("Unsupported skill effect type: Unknown");
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenTargetsAreEmpty()
    {
        var (combat, ally, _) = CreateCombat();
        var skill = CreateSkill("skill.basic.strike", "Damage", 10);

        var act = () => _resolver.Resolve(combat, ally, skill, []);

        act.Should().Throw<DomainException>().WithMessage("At least one target is required to resolve a skill effect.");
    }

    private static (Combat Combat, Combatant Ally, Combatant Enemy) CreateCombat()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);
        var combat = Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            [ally],
            [enemy]);

        return (combat, ally, enemy);
    }

    private static CombatantSkill CreateSkill(string key, string effectType, int basePower)
    {
        return CombatantSkill.Create(
            key,
            key,
            effectType,
            "SingleEnemy",
            effectType,
            0,
            0,
            basePower);
    }
}
