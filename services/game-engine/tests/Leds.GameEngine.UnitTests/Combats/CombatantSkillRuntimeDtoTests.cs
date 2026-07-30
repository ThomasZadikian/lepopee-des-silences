using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantSkillRuntimeDtoTests
{
    [Fact]
    public void FromDomain_ShouldExposeMeleeSingleTargetContract()
    {
        var dto = CombatantSkillRuntimeDto.FromDomain(CreateSkill(
            category: "Physical",
            skillType: "Damage",
            targetingType: "SingleEnemy"));

        dto.TacticalRange.Should().Be(1);
        dto.TacticalAreaShape.Should().Be("Single");
        dto.RequiresLineOfSight.Should().BeFalse();
    }

    [Fact]
    public void FromDomain_ShouldExposeRangedAreaContract()
    {
        var dto = CombatantSkillRuntimeDto.FromDomain(CreateSkill(
            category: "Magic",
            skillType: "Damage",
            targetingType: "AllEnemies"));

        dto.TacticalRange.Should().Be(4);
        dto.TacticalAreaShape.Should().Be("Diamond");
        dto.RequiresLineOfSight.Should().BeTrue();
    }

    [Fact]
    public void FromDomain_ShouldExposeSupportContractBeforeMagicCategory()
    {
        var dto = CombatantSkillRuntimeDto.FromDomain(CreateSkill(
            category: "Magic",
            skillType: "Heal",
            targetingType: "SingleAlly"));

        dto.TacticalRange.Should().Be(3);
        dto.TacticalAreaShape.Should().Be("Single");
        dto.RequiresLineOfSight.Should().BeTrue();
    }

    [Fact]
    public void FromDomain_ShouldExposeAuthoredDesignProfile()
    {
        var skill = CombatantSkill.Create(
            key: "canon.skill.flamme-froide",
            displayName: "Flamme froide",
            skillType: "Damage",
            targetingType: "SingleEnemy",
            effectType: "Damage",
            manaCost: 8,
            chargeCost: 0,
            basePower: 22,
            category: "Magic");

        var dto = CombatantSkillRuntimeDto.FromDomain(skill);

        dto.TacticalRange.Should().Be(3);
        dto.TacticalAreaShape.Should().Be("Cross");
        dto.RequiresLineOfSight.Should().BeTrue();
    }

    private static CombatantSkill CreateSkill(
        string category,
        string skillType,
        string targetingType) =>
        CombatantSkill.Create(
            key: "skill.contract.test",
            displayName: "Geste test",
            skillType: skillType,
            targetingType: targetingType,
            effectType: skillType,
            manaCost: 0,
            chargeCost: 0,
            basePower: 10,
            category: category);
}
