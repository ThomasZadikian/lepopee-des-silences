using FluentAssertions;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.UnitTests.Combats.Typing;

public sealed class EmotionalTypeProfileProviderTests
{
    private readonly EmotionalTypeProfileProvider _provider = new();

    [Fact]
    public void Resolves_enemy_natural_register_independently_of_archetype()
    {
        var fragile = Combatant.CreateEnemy(
            "enemy.x", "X", "UnrelatedArchetype", 50,
            naturalEmotionalType: EmotionalType.Melancolie);

        var profile = _provider.Resolve(fragile, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        profile.AttackType.Should().Be(EmotionalType.Melancolie);
        profile.EffectivenessAgainst(EmotionalType.Silence).Should().Be(DamageEffectiveness.Weak);
        profile.EffectivenessAgainst(EmotionalType.Memoire).Should().Be(DamageEffectiveness.Resistant);
        profile.EffectivenessAgainst(EmotionalType.Effroi).Should().Be(DamageEffectiveness.Immune);
    }

    [Fact]
    public void Resolves_hero_from_snapshotted_natural_register()
    {
        var hero = Combatant.CreateAlly(
            "unrelated.instance.key", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);

        var profile = _provider.Resolve(hero, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        profile.AttackType.Should().Be(EmotionalType.Memoire);
        profile.EffectivenessAgainst(EmotionalType.Deni).Should().Be(DamageEffectiveness.Weak);
        profile.EffectivenessAgainst(EmotionalType.Folie).Should().Be(DamageEffectiveness.Resistant);
        profile.EffectivenessAgainst(EmotionalType.Rupture).Should().Be(DamageEffectiveness.Immune);
    }

    [Fact]
    public void Unknown_combatant_resolves_to_neutral()
    {
        var unknown = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);

        var profile = _provider.Resolve(unknown, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        profile.AttackType.Should().Be(EmotionalType.Neutral);
        profile.BaseMultiplierAgainst(EmotionalType.Rupture).Should().Be(1.0);
    }

    [Fact]
    public void Catalog_register_defines_attack_type()
    {
        var attacker = Combatant.CreateEnemy(
            "enemy.x", "X", "Guard", 50,
            naturalEmotionalType: EmotionalType.Rupture);
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void Neutral_catalog_register_inherits_profile()
    {
        var attacker = Combatant.CreateEnemy(
            "enemy.x", "X", "Guard", 50,
            naturalEmotionalType: EmotionalType.Rupture);
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Neutral");

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Rupture);
    }

    [Fact]
    public void Attack_type_is_neutral_for_unknown_attacker_without_tag()
    {
        var attacker = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var skill = CombatantSkill.Create("skill.x", "X", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Neutral");

        _provider.ResolveAttackType(attacker, skill).Should().Be(EmotionalType.Neutral);
    }

    [Fact]
    public void Spell_uses_its_intrinsic_type_regardless_of_caster()
    {
        // Catalog declares skill-shadow-bite as Silence.
        // Cast by a hero whose character type is Memoire, it still deals Silence.
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void FrayeurOrganique_uses_Effroi_regardless_of_caster()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        var spell = CombatantSkill.Create(
            "canon.skill.frayeur-organique", "Frayeur organique", "Damage", "SingleEnemy", "Damage", 0, 0, 20,
            emotionalRegister: "Effroi");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Effroi);
    }

    [Fact]
    public void Tags_cannot_override_catalog_register()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            tags: ["emotype:rupture"], emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-register")]
    public void Invalid_catalog_register_is_rejected_instead_of_inheriting_from_caster(string? register)
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        var act = () =>
        {
            var skill = CombatantSkill.Rehydrate(
                "skill.invalid", "Invalid", "Damage", "SingleEnemy", "Damage", 0, 0, 10, [],
                emotionalRegister: register!);
            _provider.ResolveAttackType(hero, skill);
        };

        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("*EmotionalRegister*");
    }

    [Fact]
    public void Attack_type_override_replaces_basic_attack_type()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);
        var basicAttack = CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Neutral");

        _provider.ResolveAttackType(hero, basicAttack).Should().Be(EmotionalType.Rupture);
    }

    [Fact]
    public void Attack_type_override_keeps_innate_weaknesses()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);

        var profile = _provider.Resolve(hero, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        profile.AttackType.Should().Be(EmotionalType.Rupture);
        profile.EffectivenessAgainst(EmotionalType.Deni).Should().Be(DamageEffectiveness.Weak);
        profile.EffectivenessAgainst(EmotionalType.Rupture).Should().Be(DamageEffectiveness.Immune);
    }

    [Theory]
    [InlineData("character.thomas", EmotionalType.Silence)]
    [InlineData("character.mane", EmotionalType.Rupture)]
    [InlineData("character.mina", EmotionalType.Folie)]
    [InlineData("character.elise", EmotionalType.Melancolie)]
    [InlineData("character.john", EmotionalType.Deni)]
    public void Resolves_companion_natural_register_from_snapshot(
        string definitionKey,
        EmotionalType expected)
    {
        var companion = Combatant.CreateAlly(
            definitionKey, "Companion", "AnyRole", 100,
            naturalEmotionalType: expected);

        _provider.Resolve(companion, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create()).AttackType.Should().Be(expected);
    }

    [Fact]
    public void Attack_type_override_does_not_affect_intrinsic_spell_type()
    {
        var hero = Combatant.CreateAlly("character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        hero.ApplyAttackTypeOverride(EmotionalType.Rupture);
        var spell = CombatantSkill.Create("skill-shadow-bite", "Morsure d'Ombre", "Damage", "SingleEnemy", "Damage", 0, 0, 35,
            emotionalRegister: "Silence");

        _provider.ResolveAttackType(hero, spell).Should().Be(EmotionalType.Silence);
    }

    [Fact]
    public void Affinity_status_overrides_only_the_holder_profile()
    {
        var hero = Combatant.CreateAlly(
            "character.player.self", "Hero", "AnyRole", 100,
            naturalEmotionalType: EmotionalType.Memoire);
        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "status.memory-shield",
            "Bouclier mémoriel",
            StatusEffectKind.AffinityModifier,
            currentTick: 0,
            durationTicks: 2,
            emotionalType: EmotionalType.Deni,
            affinityOutcome: DamageEffectiveness.Immune,
            affinityPriority: 50));

        var profile = _provider.Resolve(hero, Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        profile.EffectivenessAgainst(EmotionalType.Deni).Should().Be(DamageEffectiveness.Immune);
        Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create()
            .Resolve(EmotionalType.Deni, EmotionalType.Memoire)
            .Should().Be(DamageEffectiveness.Weak);
    }
}
