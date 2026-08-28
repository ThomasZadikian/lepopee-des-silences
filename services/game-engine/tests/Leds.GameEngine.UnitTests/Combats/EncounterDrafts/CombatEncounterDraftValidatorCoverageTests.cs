using FluentAssertions;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftValidatorCoverageTests
{
    [Fact]
    public void Validate_ShouldAcceptCanonicalDraft()
    {
        FluentActions.Invoking(() => CombatEncounterDraftValidator.Validate(ValidDraft()))
            .Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectInvalidDraftEnvelope()
    {
        FluentActions.Invoking(() => CombatEncounterDraftValidator.Validate(null!))
            .Should().Throw<ArgumentNullException>();

        var valid = ValidDraft();
        AssertInvalid(valid with { RunId = Guid.Empty }, "run id");
        AssertInvalid(valid with { RoomId = Guid.Empty }, "room id");
        AssertInvalid(valid with { NodeId = Guid.Empty }, "node id");
        AssertInvalid(valid with { RoomType = " " }, "room type");
        AssertInvalid(valid with { EncounterType = " " }, "encounter type");
        AssertInvalid(valid with { RiskLevel = -1 }, "risk level");
        AssertInvalid(valid with { DifficultyMultiplier = 0 }, "difficulty multiplier");
        AssertInvalid(valid with { Allies = [] }, "at least one ally");
        AssertInvalid(valid with { Enemies = [] }, "at least one enemy");
    }

    [Fact]
    public void Validate_ShouldRejectDuplicateAndInvalidAllies()
    {
        var valid = ValidDraft();
        var ally = ValidAlly();
        var sameId = ally.CharacterInstanceId;

        AssertInvalid(valid with
        {
            Allies = [ally, ValidAlly("ally.other") with { CharacterInstanceId = sameId }]
        }, "duplicate character instance ids");

        AssertInvalid(valid with { Allies = [ally with { AllyKey = " " }] }, "definition key");
        AssertInvalid(valid with { Allies = [ally with { DisplayName = " " }] }, "display name");
        AssertInvalid(valid with { Allies = [ally with { Role = " " }] }, "role");
        AssertInvalid(valid with { Allies = [ally with { EmotionalRegister = "not-an-emotion" }] }, "emotional register");
        AssertInvalid(valid with { Allies = [ally with { CharacterInstanceId = null }] }, "character instance id");
        AssertInvalid(valid with { Allies = [ally with { CharacterInstanceId = Guid.Empty }] }, "character instance id");

        var companion = ally with { IsProtagonist = false };
        AssertInvalid(valid with { Allies = [companion with { MaxVitality = 0 }] }, "max vitality");
        AssertInvalid(valid with { Allies = [companion with { Movement = 0 }] }, "movement");

        var protagonist = ally with { IsProtagonist = true };
        AssertInvalid(valid with { Allies = [protagonist with { AttackPower = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Defense = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { StartingGuard = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Speed = 0 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Focus = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Mana = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Charge = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { MagicAttack = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { MagicDefense = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Allies = [protagonist with { Movement = 0 }] }, "invalid combat statistics");
    }

    [Fact]
    public void Validate_ShouldRejectInvalidEnemiesAndSkillResolution()
    {
        var valid = ValidDraft();
        var enemy = ValidEnemy();

        AssertInvalid(valid with { Enemies = [enemy with { EnemyKey = " " }] }, "definition key");
        AssertInvalid(valid with { Enemies = [enemy with { DisplayName = " " }] }, "display name");
        AssertInvalid(valid with { Enemies = [enemy with { Archetype = " " }] }, "archetype");
        AssertInvalid(valid with { Enemies = [enemy with { EmotionalRegister = "unknown" }] }, "emotional register");
        AssertInvalid(valid with { Enemies = [enemy with { BaseDifficulty = -1 }] }, "base difficulty");
        AssertInvalid(valid with { Enemies = [enemy with { MinRiskLevel = 5, MaxRiskLevel = 4 }] }, "risk range");
        AssertInvalid(valid with { Enemies = [enemy with { Speed = 0 }] }, "speed");
        AssertInvalid(valid with { Enemies = [enemy with { Movement = 0 }] }, "movement");
        AssertInvalid(valid with { Enemies = [enemy with { AttackPower = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Enemies = [enemy with { Defense = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Enemies = [enemy with { Focus = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Enemies = [enemy with { MagicAttack = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Enemies = [enemy with { MagicDefense = -1 }] }, "invalid combat statistics");
        AssertInvalid(valid with { Enemies = [enemy with { Mana = -1 }] }, "invalid combat statistics");

        AssertInvalid(valid with
        {
            Enemies = [enemy with { SkillKeys = ["skill.test", "SKILL.TEST"] }]
        }, "duplicate skills");
        AssertInvalid(valid with
        {
            Enemies = [enemy with { SkillKeys = ["skill.missing"] }]
        }, "unresolved skills");
    }

    [Fact]
    public void Validate_ShouldRejectMalformedSkills()
    {
        var valid = ValidDraft();
        var skill = ValidSkill();

        AssertSkillInvalid(valid, skill with { Key = " " }, "Skill key");
        AssertSkillInvalid(valid, skill with { DisplayName = " " }, "display name");
        AssertSkillInvalid(valid, skill with { SkillType = " " }, "type");
        AssertSkillInvalid(valid, skill with { TargetingType = " " }, "targeting type");
        AssertSkillInvalid(valid, skill with { EffectType = " " }, "effect type");
        AssertSkillInvalid(valid, skill with { EmotionalRegister = "unknown" }, "emotional register");
        AssertSkillInvalid(valid, skill with { ManaCost = -1 }, "negative cost");
        AssertSkillInvalid(valid, skill with { ChargeCost = -1 }, "negative cost");
        AssertSkillInvalid(valid, skill with { BasePower = -1 }, "negative cost");
        AssertSkillInvalid(valid, skill with { Cooldown = -1 }, "negative cost");
        AssertSkillInvalid(valid, skill with { TacticalRange = -1 }, "tactical range");
        AssertSkillInvalid(valid, skill with { Category = "Support" }, "category");
        AssertSkillInvalid(valid, skill with { TacticalAreaShape = "Cone" }, "area shape");

        AssertInvalid(valid with
        {
            Allies = [ValidAlly() with { Skills = [skill, skill with { Key = "SKILL.TEST" }] }]
        }, "duplicate skills");
    }

    private static void AssertSkillInvalid(
        CombatEncounterDraft valid,
        CombatEncounterDraftSkill skill,
        string expectedMessage)
    {
        AssertInvalid(valid with
        {
            Allies = [ValidAlly() with { Skills = [skill] }]
        }, expectedMessage);
    }

    private static void AssertInvalid(CombatEncounterDraft draft, string expectedMessage)
    {
        FluentActions.Invoking(() => CombatEncounterDraftValidator.Validate(draft))
            .Should().Throw<DomainException>()
            .WithMessage($"*{expectedMessage}*");
    }

    private static CombatEncounterDraft ValidDraft()
    {
        var skill = ValidSkill();
        return new CombatEncounterDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Threshold",
            RoomIndex: 1,
            RiskLevel: 2,
            EncounterType: "Combat",
            Enemies: [ValidEnemy(skill)],
            Allies: [ValidAlly(skills: [skill])],
            DifficultyMultiplier: 1.0);
    }

    private static CombatEncounterDraftAlly ValidAlly(
        string key = "ally.hero",
        IReadOnlyCollection<CombatEncounterDraftSkill>? skills = null) =>
        new(
            AllyKey: key,
            DisplayName: "Hero",
            Role: "Fighter",
            Tags: [],
            EmotionalRegister: "Neutral",
            IsProtagonist: true,
            MaxVitality: 100,
            AttackPower: 10,
            Defense: 5,
            StartingGuard: 0,
            Speed: 10,
            Initiative: 10,
            Focus: 5,
            Mana: 20,
            Charge: 0,
            Skills: skills ?? [ValidSkill()],
            MagicAttack: 0,
            MagicDefense: 0,
            Movement: 4,
            CharacterInstanceId: Guid.NewGuid());

    private static CombatEncounterDraftEnemy ValidEnemy(CombatEncounterDraftSkill? skill = null)
    {
        skill ??= ValidSkill();
        return new CombatEncounterDraftEnemy(
            EnemyKey: "enemy.test",
            DisplayName: "Enemy",
            Description: "Test enemy",
            Archetype: "Brute",
            BaseDifficulty: 1,
            MinRiskLevel: 0,
            MaxRiskLevel: 5,
            Tags: [],
            SkillKeys: [skill.Key],
            Skills: [skill],
            EmotionalRegister: "Neutral",
            AttackPower: 10,
            Defense: 5,
            Speed: 10,
            Focus: 0,
            MagicAttack: 0,
            MagicDefense: 0,
            Mana: 0,
            Movement: 4);
    }

    private static CombatEncounterDraftSkill ValidSkill() =>
        new(
            Key: "skill.test",
            DisplayName: "Strike",
            Description: "Test skill",
            SkillType: "Damage",
            TargetingType: "SingleEnemy",
            EffectType: "Damage",
            ManaCost: 0,
            ChargeCost: 0,
            BasePower: 10,
            Tags: [],
            EmotionalRegister: "Neutral",
            Category: "Physical",
            TacticalRange: 1,
            TacticalAreaShape: "Single",
            RequiresLineOfSight: false,
            Cooldown: 0);
}
