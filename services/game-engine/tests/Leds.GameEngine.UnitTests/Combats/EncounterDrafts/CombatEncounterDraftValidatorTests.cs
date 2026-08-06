using FluentAssertions;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftValidatorTests
{
    [Fact]
    public void Validate_ShouldRejectEnemyWithUnresolvedSkill()
    {
        var draft = ValidDraft() with
        {
            Enemies =
            [
                ValidDraft().Enemies.Single() with
                {
                    SkillKeys = ["skill.missing"],
                    Skills = []
                }
            ]
        };

        var act = () => CombatEncounterDraftValidator.Validate(draft);

        act.Should().Throw<DomainException>().WithMessage("*skill.missing*");
    }

    [Fact]
    public void Validate_ShouldRejectInvalidEmotionalRegister()
    {
        var draft = ValidDraft() with
        {
            Enemies = [ValidDraft().Enemies.Single() with { EmotionalRegister = "unknown" }]
        };

        var act = () => CombatEncounterDraftValidator.Validate(draft);

        act.Should().Throw<DomainException>().WithMessage("*emotional register*");
    }

    [Fact]
    public void Validate_ShouldRejectAllyWithoutCharacterInstanceIdentity()
    {
        var draft = ValidDraft() with
        {
            Allies = [ValidDraft().Allies.Single() with { CharacterInstanceId = null }]
        };

        var act = () => CombatEncounterDraftValidator.Validate(draft);

        act.Should().Throw<DomainException>().WithMessage("*character instance id*");
    }

    [Fact]
    public void Validate_ShouldRejectDuplicateCharacterInstanceIdentity()
    {
        var original = ValidDraft();
        var duplicate = original.Allies.Single() with
        {
            AllyKey = "character.companion.copy",
            IsProtagonist = false,
            MaxVitality = 80
        };
        var draft = original with { Allies = [original.Allies.Single(), duplicate] };

        var act = () => CombatEncounterDraftValidator.Validate(draft);

        act.Should().Throw<DomainException>().WithMessage("*duplicate character instance ids*");
    }

    private static CombatEncounterDraft ValidDraft()
    {
        var skill = new CombatEncounterDraftSkill(
            "skill.basic.strike", "Frappe", "Attaque", "Damage", "SingleEnemy", "Damage",
            0, 0, 10, [], EmotionalRegister: "Neutral");
        var enemy = new CombatEncounterDraftEnemy(
            "enemy.test", "Enemy", "Test", "Guard", 1, 0, 5, [], [skill.Key], [skill],
            EmotionalRegister: "Effroi");
        var ally = new CombatEncounterDraftAlly(
            "player.self", "Player", "Protagonist", [], IsProtagonist: true,
            EmotionalRegister: "Memoire", CharacterInstanceId: Guid.NewGuid());

        return new CombatEncounterDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Threshold", 0, 1, "Combat",
            [enemy], [ally]);
    }
}
