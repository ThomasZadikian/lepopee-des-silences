using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

/// <summary>
/// Room/weather-conditional equipment StatBonus/StatBonusPercent effects (e.g. Boussole du
/// Pèlerin's "+10% Vitesse dans la Montagne", Couronne de sel's weather-gated magic attack) —
/// re-evaluated fresh every combat by CombatFactory instead of being baked into
/// PlayerStatMerger's static run-start stats. See CombatFactory.ApplyConditionalEquipmentStatBundle.
/// </summary>
public sealed class CombatFactoryConditionalEquipmentTests
{
    private static CombatEncounterDraft CreateDraftWithProtagonist()
    {
        var protagonist = new CombatEncounterDraftAlly(
            "player.self", "Le Porteur", "Fighter", Array.Empty<string>(), IsProtagonist: true,
            EmotionalRegister: "Memoire", CharacterInstanceId: Guid.NewGuid());

        var enemy = new CombatEncounterDraftEnemy(
            "enemy.0", "Enemy0", "Description", "Guard", 3, 1, 5,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CombatEncounterDraftSkill>(),
            EmotionalRegister: "Effroi");

        return new CombatEncounterDraft(
            RunId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            RoomType: "Threshold",
            RoomIndex: 1,
            RiskLevel: 3,
            EncounterType: "Combat",
            Enemies: new[] { enemy },
            Allies: new[] { protagonist });
    }

    [Fact]
    public void BuildRoster_ShouldAttachCatalogRuntimeBehaviorWithItsSource()
    {
        var effect = new CatalogItemEquipmentEffect(
            "RuntimeBehavior", null, null, null, null,
            BehaviorCode: "reflect-first-melee-hit",
            SourceDefinitionKey: "item.cornes-ivoire");

        var roster = new CombatFactory().BuildRoster(
            CombatId.New(), CreateDraftWithProtagonist(),
            conditionalEquipmentEffects: [effect]);

        roster.Allies.Single().EquipmentBehaviorCodes
            .Should().Contain("reflect-first-melee-hit|item.cornes-ivoire");
    }

    [Fact]
    public void BuildRoster_ShouldApplyEquipmentEffectsToTheMatchingCharacterInstanceOnly()
    {
        var protagonistId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var draft = CreateDraftWithProtagonist() with
        {
            Allies =
            [
                CreateDraftWithProtagonist().Allies.Single() with
                {
                    CharacterInstanceId = protagonistId
                },
                new CombatEncounterDraftAlly(
                    "character.mane", "Mané", "Companion", [],
                    MaxVitality: 80,
                    EmotionalRegister: "Rupture",
                    CharacterInstanceId: companionId)
            ]
        };
        var effect = new CatalogItemEquipmentEffect(
            "RuntimeBehavior", null, null, null, null,
            BehaviorCode: "reflect-first-melee-hit",
            SourceDefinitionKey: "item.cornes-ivoire");

        var roster = new CombatFactory().BuildRoster(
            CombatId.New(), draft,
            equipmentEffectsByCharacterId:
                new Dictionary<Guid, IReadOnlyCollection<CatalogItemEquipmentEffect>>
                {
                    [companionId] = [effect]
                });

        roster.Allies.Single(ally => ally.CharacterInstanceId == protagonistId)
            .EquipmentBehaviorCodes.Should().BeEmpty();
        roster.Allies.Single(ally => ally.CharacterInstanceId == companionId)
            .EquipmentBehaviorCodes.Should().Contain("reflect-first-melee-hit|item.cornes-ivoire");
    }

    [Fact]
    public void BuildRoster_ShouldApplyRoomConditionalBonus_WhenRoomThemeMatches()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "Speed", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, speed: 10, roomTheme: "Montagne",
            conditionalEquipmentEffects: effects);

        var protagonist = roster.Allies.Single();
        protagonist.EffectiveSpeed.Should().Be(11);
    }

    [Fact]
    public void BuildRoster_ShouldNotApplyRoomConditionalBonus_WhenRoomThemeDiffers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "Speed", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, speed: 10, roomTheme: "Jardin",
            conditionalEquipmentEffects: effects);

        var protagonist = roster.Allies.Single();
        protagonist.EffectiveSpeed.Should().Be(10);
        protagonist.StatusEffects.Should().BeEmpty();
    }

    [Fact]
    public void BuildRoster_ShouldApplyFlatRoomConditionalBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonus", StatKind: "AttackPower", Amount: 5, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, attackPower: 20, roomTheme: "Montagne",
            conditionalEquipmentEffects: effects);

        var protagonist = roster.Allies.Single();
        protagonist.EffectiveAttackPower.Should().Be(25);
        protagonist.StatusEffects.Should().Contain(effect =>
            effect.Kind == StatusEffectKind.StatModifier
            && effect.Stat == CombatStat.AttackPower
            && effect.Magnitude == 5
            && !effect.IsMagnitudePercentOfBaseStat);
    }

    [Fact]
    public void BuildRoster_ShouldIgnoreConditionalBonus_WhenNoRoomThemeOrClimateProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "Speed", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, speed: 10, conditionalEquipmentEffects: effects);

        roster.Allies.Single().EffectiveSpeed.Should().Be(10);
    }

    [Fact]
    public void BuildRoster_ShouldApplyWeatherConditionalBonus_WhenClimateMatches()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        // 8 => RoomClimate.PluieViolacee (see CombatFactory.ResolveActiveClimate).
        var climateModifier = RunModifier.Create(
            RunModifierType.RoomClimate, 8, RunModifierDuration.UntilRoomEnds,
            sourceType: "law", sourceKey: "law.marees-hautes", expiresAtRoomId: draft.RoomId);
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "MagicAttack", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "weather:PluieViolacee"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, magicAttack: 20, runModifiers: [climateModifier],
            conditionalEquipmentEffects: effects);

        roster.Allies.Single().EffectiveMagicAttack.Should().Be(22);
    }

    [Fact]
    public void BuildRoster_ShouldNotApplyWeatherConditionalBonus_WhenClimateDiffers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        // 9 => RoomClimate.Accalmie, not PluieViolacee.
        var climateModifier = RunModifier.Create(
            RunModifierType.RoomClimate, 9, RunModifierDuration.UntilRoomEnds,
            sourceType: "law", sourceKey: "law.repit", expiresAtRoomId: draft.RoomId);
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "MagicAttack", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "weather:PluieViolacee"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, magicAttack: 20, runModifiers: [climateModifier],
            conditionalEquipmentEffects: effects);

        roster.Allies.Single().EffectiveMagicAttack.Should().Be(20);
    }

    [Fact]
    public void BuildRoster_ShouldApplyConditionalMaxVitalityBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "MaxVitality", Amount: 5, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var roster = factory.BuildRoster(
            CombatId.New(), draft, roomTheme: "Montagne", conditionalEquipmentEffects: effects);

        roster.Allies.Single().MaxVitality.Should().Be(105);
    }
}
