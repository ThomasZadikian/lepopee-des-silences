using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class PlayerRuntimeSkillCoverageTests
{
    [Fact]
    public void Create_ShouldTrimIdentityAndDefaultBlankCategory()
    {
        var skill = PlayerRuntimeSkill.Create(
            " skill.test ", " Skill ", "Damage", "SingleEnemy", "Damage",
            1, 2, 10, category: " ", emotionalRegister: "Neutral");

        skill.Key.Should().Be("skill.test");
        skill.DisplayName.Should().Be("Skill");
        skill.Category.Should().Be("Physical");
        skill.BasePower.Should().Be(10);
    }

    [Theory]
    [InlineData("", "Skill", 10, 1, 0, "key")]
    [InlineData("skill.test", "", 10, 1, 0, "display name")]
    [InlineData("skill.test", "Skill", -1, 1, 0, "base power")]
    [InlineData("skill.test", "Skill", 10, -1, 0, "tactical range")]
    [InlineData("skill.test", "Skill", 10, 1, -1, "cooldown")]
    public void Create_ShouldRejectInvalidInputs(
        string key,
        string displayName,
        int basePower,
        int tacticalRange,
        int cooldown,
        string expected)
    {
        var action = () => PlayerRuntimeSkill.Create(
            key, displayName, "Damage", "SingleEnemy", "Damage",
            0, 0, basePower,
            tacticalRange: tacticalRange,
            cooldown: cooldown,
            emotionalRegister: "Neutral");

        action.Should().Throw<DomainException>().WithMessage($"*{expected}*");
    }

    [Fact]
    public void Create_ShouldPreserveNonBlankCategoryAndTacticalMetadata()
    {
        var skill = PlayerRuntimeSkill.Create(
            "skill.test", "Skill", "Damage", "Area", "Damage",
            3, 4, 15,
            category: "Magic",
            basePowerIsPercentOfMaxVitality: true,
            tacticalRange: 5,
            tacticalAreaShape: "Cross",
            requiresLineOfSight: true,
            cooldown: 2,
            isUltimate: true,
            emotionalRegister: "Rupture");

        skill.Category.Should().Be("Magic");
        skill.BasePowerIsPercentOfMaxVitality.Should().BeTrue();
        skill.TacticalRange.Should().Be(5);
        skill.TacticalAreaShape.Should().Be("Cross");
        skill.RequiresLineOfSight.Should().BeTrue();
        skill.Cooldown.Should().Be(2);
        skill.IsUltimate.Should().BeTrue();
        skill.EmotionalRegister.Should().Be("Rupture");
    }

    [Fact]
    public void Rehydrate_ShouldRetainTrustedValuesAndNormalizeEmotionalRegister()
    {
        var skill = PlayerRuntimeSkill.Rehydrate(
            " persisted ", " Persisted ", "Support", "Self", "Heal",
            manaCost: 2, chargeCost: 1, basePower: 5,
            category: "Support",
            tacticalRange: 0,
            emotionalRegister: "Neutral");

        skill.Key.Should().Be(" persisted ");
        skill.DisplayName.Should().Be(" Persisted ");
        skill.Category.Should().Be("Support");
        skill.EmotionalRegister.Should().Be("Neutral");
    }
}
