using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

/// <summary>
/// Grimoire mid-run skill resync ("Valider les choix") — <see cref="Run.ReplacePlayerSkills"/>
/// (protagonist) and <see cref="Run.ReplaceCharacterSkills"/> (companions).
/// </summary>
public sealed class RunReplaceSkillsTests
{
    private static PlayerRuntimeSkill CreateSkill(string key = "skill.basic.guard") =>
        PlayerRuntimeSkill.Create(
            key: key,
            displayName: "Garde",
            skillType: "Defense",
            targetingType: "Self",
            effectType: "Guard",
            manaCost: 0,
            chargeCost: 0,
            basePower: 5,
            emotionalRegister: "Neutral");

    [Fact]
    public void ReplacePlayerSkills_ShouldReplaceTheProtagonistLoadout()
    {
        var run = TestGameEngineFactory.CreateRun();
        var newSkill = CreateSkill("skill.new.equipped");

        run.ReplacePlayerSkills([newSkill]);

        run.PlayerState.Skills.Should().ContainSingle(s => s.Key == "skill.new.equipped");
    }

    [Fact]
    public void ReplacePlayerSkills_ShouldThrow_WhenGivenNoSkills()
    {
        var run = TestGameEngineFactory.CreateRun();

        var act = () => run.ReplacePlayerSkills([]);

        act.Should().Throw<DomainException>().WithMessage("*at least one skill*");
    }

    [Fact]
    public void ReplaceCharacterSkills_ShouldReplaceTheMatchingCompanionLoadout()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var character = run.PlayerSnapshot!.Characters.First();
        var newSkill = RunCharacterSkillSnapshot.Create(
            skillDefinitionKey: "skill.new.equipped",
            displayName: "Nouveau sort",
            skillType: "Damage",
            targetingMode: "SingleEnemy",
            effectType: "Damage",
            manaCost: 0,
            chargeCost: 0,
            basePower: 8,
            emotionalRegister: "Neutral");

        run.ReplaceCharacterSkills(character.CharacterId, [newSkill]);

        character.Skills.Should().ContainSingle(s => s.SkillDefinitionKey == "skill.new.equipped");
    }

    [Fact]
    public void ReplaceCharacterSkills_ShouldThrow_WhenCharacterIsNotInTheSnapshot()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var newSkill = RunCharacterSkillSnapshot.Create(
            skillDefinitionKey: "skill.new.equipped",
            displayName: "Nouveau sort",
            skillType: "Damage",
            targetingMode: "SingleEnemy",
            effectType: "Damage",
            emotionalRegister: "Neutral");

        var act = () => run.ReplaceCharacterSkills(Guid.NewGuid(), [newSkill]);

        act.Should().Throw<DomainException>().WithMessage("*was not found*");
    }

    [Fact]
    public void ReplaceCharacterSkills_ShouldThrow_WhenGivenNoSkills()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var character = run.PlayerSnapshot!.Characters.First();

        var act = () => run.ReplaceCharacterSkills(character.CharacterId, []);

        act.Should().Throw<DomainException>().WithMessage("*at least one skill*");
    }
}
