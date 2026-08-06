using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalSkillProfileTests
{
    [Fact]
    public void For_ShouldUseCatalogContract()
    {
        var profile = TacticalSkillProfile.For(CreateSkill(
            "canon.skill.test",
            category: "Physical",
            skillType: "Damage",
            targetingType: "SingleEnemy",
            tacticalRange: 3,
            tacticalAreaShape: TacticalAreaShape.Cross,
            requiresLineOfSight: true));

        profile.Should().Be(new TacticalSkillProfile(
            Range: 3,
            AreaShape: TacticalAreaShape.Cross,
            RequiresLineOfSight: true));
    }

    [Fact]
    public void For_ShouldKeepSelfTargetingOnTheCasterCell()
    {
        var profile = TacticalSkillProfile.For(CreateSkill(
            "canon.skill.self",
            category: "Magic",
            skillType: "Buff",
            targetingType: "Self",
            tacticalRange: 0));

        profile.Range.Should().Be(0);
        profile.AreaShape.Should().Be(TacticalAreaShape.Single);
        profile.RequiresLineOfSight.Should().BeFalse();
    }

    private static CombatantSkill CreateSkill(
        string key,
        string category,
        string skillType,
        string targetingType,
        int tacticalRange = 1,
        TacticalAreaShape tacticalAreaShape = TacticalAreaShape.Single,
        bool requiresLineOfSight = false) =>
        CombatantSkill.Create(
            key: key,
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
            requiresLineOfSight: requiresLineOfSight, emotionalRegister: "Neutral");
}
