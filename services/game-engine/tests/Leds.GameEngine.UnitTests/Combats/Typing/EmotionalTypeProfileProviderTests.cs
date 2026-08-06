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
        profile.WeakTo.Should().Contain(EmotionalType.Silence);
        profile.ResistantTo.Should().Contain(EmotionalType.Memoire);
        profile.ImmuneTo.Should().Contain(EmotionalType.Effroi);
    }

    [Fact]
    public void Resolves_hero_by_source_key()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);

        var profile = _provider.Resolve(hero);

        profile.AttackType.Should().Be(EmotionalType.Memoire);
        profile.WeakTo.Should().Contain(EmotionalType.Deni);
        profile.ResistantTo.Should().Contain(EmotionalType.Folie);
        profile.ImmuneTo.Should().Contain(EmotionalType.Rupture);
    }

    [Fact]
    public void Unknown_combatant_resolves_to_neutral()
    {
        var unknown = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);

        _provider.Resolve(unknown).Should().BeSameAs(CombatantTypeProfile.Neutral);
    }

    [Fact]
    public void Catalog_register_defines_attack_type()
    {
        var attacker = Combatant.CreateEnemy("enemy.x", "X", "Guard", 50); // profile attack = Rupture
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void Neutral_catalog_register_inherits_profile()
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

    [Fact]
    public void Spell_uses_its_intrinsic_type_regardless_of_caster()
    {
        // Catalog declares skill-shadow-bite as Silence.
        // Cast by a hero whose character type is Memoire, it still deals Silence.
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void FrayeurOrganique_uses_Effroi_regardless_of_caster()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        var spell = CombatantSkill.Create(
            "canon.skill.frayeur-organique", "Frayeur organique", "Damage", "SingleEnemy", "Damage", 0, 0, 20,
            emotionalRegister: "Effroi");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Effroi);
    }

    [Fact]
    public void Tags_cannot_override_catalog_register()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            tags: ["emotype:rupture"], emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void Attack_type_override_replaces_basic_attack_type()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);
        var basicAttack = CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10);

        _provider.ResolveAttackType(hero, basicAttack).Should().Be(EmotionalType.Rupture);
    }

    [Fact]
    public void Attack_type_override_keeps_innate_weaknesses()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);

        var profile = _provider.Resolve(hero);

        profile.AttackType.Should().Be(EmotionalType.Rupture);
        profile.WeakTo.Should().Contain(EmotionalType.Deni);
        profile.ImmuneTo.Should().Contain(EmotionalType.Rupture);
    }

    [Theory]
    [InlineData("character.thomas", EmotionalType.Silence)]
    [InlineData("character.mane", EmotionalType.Rupture)]
    [InlineData("character.mina", EmotionalType.Folie)]
    [InlineData("character.elise", EmotionalType.Melancolie)]
    [InlineData("character.john", EmotionalType.Deni)]
    public void Resolves_companion_natural_register_from_definition_key(
        string definitionKey,
        EmotionalType expected)
    {
        var companion = Combatant.CreateAlly(definitionKey, "Companion", "AnyRole", 100);

        _provider.Resolve(companion).AttackType.Should().Be(expected);
    }

    [Fact]
    public void Attack_type_override_does_not_affect_intrinsic_spell_type()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }
}
