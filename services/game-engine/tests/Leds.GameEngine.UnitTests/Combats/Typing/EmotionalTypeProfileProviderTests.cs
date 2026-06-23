using FluentAssertions;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats.Typing;

public sealed class EmotionalTypeProfileProviderTests
{
    private readonly EmotionalTypeProfileProvider _provider = new();

    [Fact]
    public void Resolves_enemy_archetype_profile()
    {
        var fragile = Combatant.CreateEnemy("enemy.x", "X", "Fragile", 50);

        var profile = _provider.Resolve(fragile);

        profile.AttackType.Should().Be(EmotionalType.Melancolie);
        profile.WeakTo.Should().Contain(EmotionalType.Rupture);
    }

    [Fact]
    public void Resolves_hero_by_source_key()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);

        var profile = _provider.Resolve(hero);

        profile.AttackType.Should().Be(EmotionalType.Memoire);
        profile.WeakTo.Should().Contain(EmotionalType.Effroi);
    }

    [Fact]
    public void Unknown_combatant_resolves_to_neutral()
    {
        var unknown = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);

        _provider.Resolve(unknown).Should().BeSameAs(CombatantTypeProfile.Neutral);
    }

    [Fact]
    public void Skill_tag_overrides_attack_type()
    {
        var attacker = Combatant.CreateEnemy("enemy.x", "X", "Guard", 50); // profile attack = Rupture
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10, ["emotype:silence"]);

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void Attack_type_falls_back_to_profile_when_no_tag()
    {
        var attacker = Combatant.CreateEnemy("enemy.x", "X", "Guard", 50);
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10);

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Rupture);
    }

    [Fact]
    public void Attack_type_is_neutral_for_unknown_attacker_without_tag()
    {
        var attacker = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10);

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Neutral);
    }
}