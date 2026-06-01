using FluentAssertions;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.UnitTests.Skills;

public sealed class SkillTemplateTests
{
    [Fact]
    public void Create_ShouldCreateDamageSkillTemplate_WhenInputIsValid()
    {
        var skill = SkillTemplate.Create(
            "skill-shadow-bite",
            "Morsure d’Ombre",
            "Une attaque de rupture silencieuse.",
            "catalog-0.1.0",
            CombatElement.Shadow,
            SkillEffectType.Damage,
            SkillTargetType.SingleEnemy,
            manaCost: 3,
            chargeCost: 1,
            basePower: 35,
            healPower: 0);

        skill.Id.Value.Should().NotBeEmpty();
        skill.Key.Value.Should().Be("skill-shadow-bite");
        skill.Name.Value.Should().Be("Morsure d’Ombre");
        skill.Description.Value.Should().Be("Une attaque de rupture silencieuse.");
        skill.Version.Value.Should().Be("catalog-0.1.0");
        skill.Status.Should().Be(CatalogContentStatus.Draft);
        skill.Element.Should().Be(CombatElement.Shadow);
        skill.EffectType.Should().Be(SkillEffectType.Damage);
        skill.TargetType.Should().Be(SkillTargetType.SingleEnemy);
        skill.ManaCost.Should().Be(3);
        skill.ChargeCost.Should().Be(1);
        skill.BasePower.Should().Be(35);
        skill.HealPower.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldCreateHealSkillTemplate_WhenInputIsValid()
    {
        var skill = SkillTemplate.Create(
            "skill-memory-mend",
            "Suture de Mémoire",
            "Restaure une partie de soi.",
            "catalog-0.1.0",
            CombatElement.Memory,
            SkillEffectType.Heal,
            SkillTargetType.SingleAlly,
            manaCost: 4,
            chargeCost: 1,
            basePower: 0,
            healPower: 25);

        skill.EffectType.Should().Be(SkillEffectType.Heal);
        skill.HealPower.Should().Be(25);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenDamageSkillHasNoBasePower()
    {
        var act = () => SkillTemplate.Create(
            "skill-shadow-bite",
            "Morsure d’Ombre",
            "Description",
            "catalog-0.1.0",
            CombatElement.Shadow,
            SkillEffectType.Damage,
            SkillTargetType.SingleEnemy,
            manaCost: 3,
            chargeCost: 1,
            basePower: 0,
            healPower: 0);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Damage skill must have a base power greater than 0.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenManaCostIsNegative()
    {
        var act = () => SkillTemplate.Create(
            "skill-shadow-bite",
            "Morsure d’Ombre",
            "Description",
            "catalog-0.1.0",
            CombatElement.Shadow,
            SkillEffectType.Damage,
            SkillTargetType.SingleEnemy,
            manaCost: -1,
            chargeCost: 1,
            basePower: 35,
            healPower: 0);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Skill mana cost cannot be negative.");
    }
}