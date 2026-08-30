using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantSkillTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var skill = CombatantSkill.Create(
            "skill.basic.strike",
            "Frappe",
            "Damage",
            "SingleEnemy",
            "Damage",
            5,
            0,
            10,
            ["basic"], emotionalRegister: "Neutral");

        skill.Key.Should().Be("skill.basic.strike");
        skill.DisplayName.Should().Be("Frappe");
        skill.SkillType.Should().Be("Damage");
        skill.TargetingType.Should().Be("SingleEnemy");
        skill.EffectType.Should().Be("Damage");
        skill.ManaCost.Should().Be(5);
        skill.ChargeCost.Should().Be(0);
        skill.BasePower.Should().Be(10);
        skill.Tags.Should().BeEquivalentTo(["basic"]);
    }

    [Fact]
    public void Create_ShouldThrow_WhenKeyIsEmpty()
    {
        var act = () => CombatantSkill.Create("", "Frappe", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("Combatant skill key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDisplayNameIsEmpty()
    {
        var act = () => CombatantSkill.Create("skill.basic.strike", "", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("Combatant skill display name is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenBasePowerIsNegative()
    {
        var act = () => CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 5, 0, -1,
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("Combatant skill base power must be non-negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenManaCostIsNegative()
    {
        var act = () => CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", -1, 0, 10,
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("Combatant skill mana cost must be non-negative.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenChargeCostIsNegative()
    {
        var act = () => CombatantSkill.Create("skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 5, -1, 10,
            emotionalRegister: "Neutral");

        act.Should().Throw<DomainException>().WithMessage("Combatant skill charge cost must be non-negative.");
    }
}
