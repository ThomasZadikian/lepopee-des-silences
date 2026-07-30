using FluentAssertions;
using Leds.GameEngine.Application.Combats.Dtos;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatantSkillRuntimeDtoTests
{
    [Fact]
    public void FromDomain_ShouldExposeMandatoryTacticalContract()
    {
        var dto = CombatantSkillRuntimeDto.FromDomain(CreateSkill(
            category: "Physical",
            skillType: "Damage",
            targetingType: "SingleEnemy",
            tacticalRange: 2,
            tacticalAreaShape: TacticalAreaShape.Cross,
            requiresLineOfSight: true));

        dto.TacticalRange.Should().Be(2);
        dto.TacticalAreaShape.Should().Be("Cross");
        dto.RequiresLineOfSight.Should().BeTrue();
    }

    [Fact]
    public void FromDomain_ShouldExposeSupportContractBeforeMagicCategory()
    {
        var dto = CombatantSkillRuntimeDto.FromDomain(CreateSkill(
            category: "Magic",
            skillType: "Heal",
            targetingType: "SingleAlly",
            tacticalRange: 3,
            requiresLineOfSight: true));

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
            category: "Magic",
            tacticalRange: 3,
            tacticalAreaShape: TacticalAreaShape.Cross,
            requiresLineOfSight: true);

        var dto = CombatantSkillRuntimeDto.FromDomain(skill);

        dto.TacticalRange.Should().Be(3);
        dto.TacticalAreaShape.Should().Be("Cross");
        dto.RequiresLineOfSight.Should().BeTrue();
    }

    private static CombatantSkill CreateSkill(
        string category,
        string skillType,
        string targetingType,
        int tacticalRange = 1,
        TacticalAreaShape tacticalAreaShape = TacticalAreaShape.Single,
        bool requiresLineOfSight = false) =>
        CombatantSkill.Create(
            key: "skill.contract.test",
            displayName: "Geste test",
            skillType: skillType,
            targetingType: targetingType,
            effectType: skillType,
            manaCost: 0,
            chargeCost: 0,
            basePower: 10,
            category: category,
            tacticalRange: tacticalRange,
            tacticalAreaShape: tacticalAreaShape,
            requiresLineOfSight: requiresLineOfSight);
}
