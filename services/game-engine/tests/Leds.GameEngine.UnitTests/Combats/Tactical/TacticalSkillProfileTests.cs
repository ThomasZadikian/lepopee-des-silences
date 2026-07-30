using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalSkillProfileTests
{
    [Theory]
    [InlineData("canon.skill.rempart", 2, TacticalAreaShape.Cross, true)]
    [InlineData("canon.skill.flamme-froide", 3, TacticalAreaShape.Cross, true)]
    [InlineData("canon.skill.berceuse-inversee", 3, TacticalAreaShape.Diamond, true)]
    [InlineData("canon.skill.frappe-denclume", 1, TacticalAreaShape.Cross, false)]
    [InlineData("canon.skill.silence-partage", int.MaxValue, TacticalAreaShape.Map, false)]
    public void For_ShouldUseAuthoredDesignProfile(
        string skillKey,
        int expectedRange,
        TacticalAreaShape expectedShape,
        bool expectedLineOfSight)
    {
        var profile = TacticalSkillProfile.For(CreateSkill(
            skillKey,
            category: "Physical",
            skillType: "Damage",
            targetingType: "SingleEnemy"));

        profile.Range.Should().Be(expectedRange);
        profile.AreaShape.Should().Be(expectedShape);
        profile.RequiresLineOfSight.Should().Be(expectedLineOfSight);
        profile.OncePerCombat.Should().Be(skillKey == "canon.skill.silence-partage");
    }

    [Fact]
    public void For_ShouldDeriveFallbackForSkillsWithoutAuthoredProfile()
    {
        var profile = TacticalSkillProfile.For(CreateSkill(
            "canon.skill.non-authoree",
            category: "Magic",
            skillType: "Damage",
            targetingType: "AllEnemies"));

        profile.Should().Be(new TacticalSkillProfile(
            Range: TacticalRange.Ranged,
            AreaShape: TacticalAreaShape.Diamond,
            RequiresLineOfSight: true,
            OncePerCombat: false));
    }

    [Fact]
    public void For_ShouldKeepSelfTargetingOnTheCasterCell()
    {
        var profile = TacticalSkillProfile.For(CreateSkill(
            "canon.skill.self",
            category: "Magic",
            skillType: "Buff",
            targetingType: "Self"));

        profile.Range.Should().Be(0);
        profile.AreaShape.Should().Be(TacticalAreaShape.Single);
        profile.RequiresLineOfSight.Should().BeFalse();
    }

    private static CombatantSkill CreateSkill(
        string key,
        string category,
        string skillType,
        string targetingType) =>
        CombatantSkill.Create(
            key: key,
            displayName: "Geste test",
            skillType: skillType,
            targetingType: targetingType,
            effectType: skillType,
            manaCost: 0,
            chargeCost: 0,
            basePower: 10,
            category: category);
}
