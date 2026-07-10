using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatFactoryTests
{
    private static CombatEncounterDraft CreateDraft(
        int allyCount = 1,
        int enemyCount = 1,
        int enemyBaseDifficulty = 3,
        bool includeSkills = false,
        IReadOnlyCollection<CombatEncounterDraftSkill>? enemySkills = null,
        IReadOnlyCollection<string>? allyTags = null,
        int enemyAttackPower = 0,
        int enemyDefense = 0,
        int enemySpeed = 10,
        int enemyFocus = 0,
        double difficultyMultiplier = 1.0)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            new CombatEncounterDraftAlly(
                $"player.{i}",
                $"Hero{i}",
                "Fighter",
                allyTags ?? Array.Empty<string>())).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
        {
            var skills = enemySkills ?? (includeSkills
                ? new[]
                {
                    new CombatEncounterDraftSkill(
                        "skill.basic.strike", "Frappe", "Attack.", "Damage", "SingleEnemy", "Damage", 5, 0, 10,
                        Array.Empty<string>())
                }
                : Array.Empty<CombatEncounterDraftSkill>());

            return new CombatEncounterDraftEnemy(
                $"enemy.{i}",
                $"Enemy{i}",
                $"Description {i}",
                "Guard",
                enemyBaseDifficulty,
                1,
                5,
                Array.Empty<string>(),
                skills.Select(s => s.Key).ToArray(),
                skills,
                AttackPower: enemyAttackPower,
                Defense: enemyDefense,
                Speed: enemySpeed,
                Focus: enemyFocus);
        }).ToArray();

        return new CombatEncounterDraft(
            RunId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            RoomType: "Threshold",
            RoomIndex: 1,
            RiskLevel: 3,
            EncounterType: "Combat",
            Enemies: enemies,
            Allies: allies,
            DifficultyMultiplier: difficultyMultiplier);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyAttackTypeOverride_WhenModifierProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyTags: new[] { "player" });

        var modifier = RunModifier.Create(
            RunModifierType.AttackTypeOverride,
            (int)EmotionalType.Rupture,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.relic.rupture-mask");

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier });

        combat.Allies.Single().AttackTypeOverride.Should().Be(EmotionalType.Rupture);
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateCombat()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Should().NotBeNull();
        combat.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateFromDraft_ShouldUseProvidedCombatId()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var combatId = CombatId.New();

        var combat = factory.CreateFromDraft(combatId, draft);

        combat.Id.Should().Be(combatId);
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateAlliesFromDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        combat.Allies.Should().HaveCount(2);
        combat.Allies.Select(a => a.SourceKey).Should()
            .Contain("player.0").And.Contain("player.1");
    }

    [Fact]
    public void CreateFromDraft_ShouldCreateEnemiesFromDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Should().HaveCount(2);
        combat.Enemies.Select(e => e.SourceKey).Should()
            .Contain("enemy.0").And.Contain("enemy.1");
    }

    [Fact]
    public void CreateFromDraft_ShouldMapEnemySkills()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.Skills.Should().HaveCount(1);
        enemy.Skills.Single().Key.Should().Be("skill.basic.strike");
        enemy.Skills.Single().DisplayName.Should().Be("Frappe");
    }

    [Fact]
    public void CreateFromDraft_ShouldMapEnemySkillCategory()
    {
        var factory = new CombatFactory();
        var magicSkill = new CombatEncounterDraftSkill(
            "canon.skill.flamme-froide", "Flamme froide", "Cold flame.", "Damage", "SingleEnemy", "Damage", 8, 0, 22,
            Array.Empty<string>(), Category: "Magic");
        var draft = CreateDraft(enemySkills: [magicSkill]);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Single().Skills.Single().Category.Should().Be("Magic");
    }

    [Fact]
    public void CreateFromDraft_ShouldDefaultEnemySkillCategoryToPhysical_WhenNotSpecified()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Single().Skills.Single().Category.Should().Be("Physical");
    }

    [Fact]
    public void CreateFromDraft_ShouldNormalizeEnemyCurrentGuardSkill()
    {
        var factory = new CombatFactory();
        var guardSkill = new CombatEncounterDraftSkill(
            "skill.basic.guard", "Garde", "Guard.", "Defense", "Self", "AddCurrentGuard", 0, 0, 5,
            Array.Empty<string>());
        var draft = CreateDraft(enemySkills: [guardSkill]);

        var combat = factory.CreateFromDraft(draft);

        var skill = combat.Enemies.Single().Skills.Single();
        skill.EffectType.Should().Be("Guard");
        skill.BasePower.Should().Be(5);
    }

    [Fact]
    public void CreateFromDraft_ShouldInitializeEnemyVitalityFromBaseDifficulty()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyBaseDifficulty: 5);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.MaxVitality.Should().Be(40 + 5 * 10);
        enemy.CurrentVitality.Should().Be(enemy.MaxVitality);
    }

    [Fact]
    public void CreateFromDraft_ShouldWireCatalogAttackDefenseSpeedFocus_IntoEnemyCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyAttackPower: 15, enemyDefense: 8, enemySpeed: 12, enemyFocus: 6);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.BaseStatSnapshot.AttackPower.Should().Be(15);
        enemy.BaseStatSnapshot.Defense.Should().Be(8);
        enemy.BaseStatSnapshot.Speed.Should().Be(12);
        enemy.BaseStatSnapshot.Focus.Should().Be(6);
    }

    [Fact]
    public void CreateFromDraft_ShouldScaleEnemyAttackDefenseSpeedFocus_WithDifficultyMultiplier()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(
            enemyAttackPower: 10, enemyDefense: 10, enemySpeed: 12, enemyFocus: 5, difficultyMultiplier: 2.0);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.BaseStatSnapshot.AttackPower.Should().Be(20,
            because: "Attack should keep pace with run depth, same as Vitality.");
        enemy.BaseStatSnapshot.Defense.Should().Be(20,
            because: "Defense should keep pace with run depth, same as Vitality.");
        enemy.BaseStatSnapshot.Speed.Should().Be(24,
            because: "Speed now keeps pace with run depth too, same as the other authored stats.");
        enemy.BaseStatSnapshot.Focus.Should().Be(10,
            because: "Focus (crit chance) should keep pace with run depth, same as the other authored stats.");
    }

    [Fact]
    public void CreateFromDraft_ShouldInitializePlayerVitality()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.MaxVitality.Should().Be(100);
        ally.CurrentVitality.Should().Be(100);
    }

    [Fact]
    public void CreateFromDraft_ShouldGiveDefaultSkillsToAllies()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.Skills.Select(s => s.Key).Should()
            .Contain("skill.basic.strike")
            .And.Contain("skill.basic.guard");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyAttackPowerBonusToPlayerDamageSkills()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyTags: new[] { "player" });

        var modifier = RunModifier.Create(
            RunModifierType.AttackPowerBonus,
            0.10,
            RunModifierDuration.UntilRunEnds,
            "PalaceLaw",
            "law-carnage-v1");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        var ally = combat.Allies.Single();
        ally.Skills.Single(skill => skill.Key == "skill.basic.strike").BasePower.Should().Be(11);
        ally.Skills.Single(skill => skill.Key == "skill.basic.guard").BasePower.Should().Be(5);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyTeamSpeedBonusModifier_ToAllyBaseSpeed()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyTags: new[] { "player" });

        var modifier = RunModifier.Create(
            RunModifierType.SpeedBonus,
            0.10,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "canon.item.reve-erina");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        var ally = combat.Allies.Single();
        ally.BaseStatSnapshot.Speed.Should().Be(11,
            because: "Rêve d'Erina grants +10% team Speed while carried (default draft Speed is 10).");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyRainClimateStartingGuardBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 2)]);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(5);
        ally.BaseGuard.Should().Be(5);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyHeatwaveClimateEnemyPowerBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var baseline = factory.CreateFromDraft(draft);
        var heatwave = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 3)]);

        var baselinePower = baseline.Enemies.Single().Skills.Single().BasePower;
        var heatwavePower = heatwave.Enemies.Single().Skills.Single().BasePower;
        heatwavePower.Should().BeGreaterThan(baselinePower);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetCombatActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetTurnNumberToOne()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void CreateFromDraft_ShouldSetFirstAllyAsActiveCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        var expectedActiveCombatantId = combat.Allies.Concat(combat.Enemies)
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .First().Id;
        combat.ActiveCombatantId.Should().Be(expectedActiveCombatantId);
    }

    [Fact]
    public void CreateFromDraft_ShouldUseSpeed_ForInitialActiveCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft, speed: 1);

        var expectedActiveCombatantId = combat.Allies.Concat(combat.Enemies)
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .First()
            .Id;

        combat.ActiveCombatantId.Should().Be(expectedActiveCombatantId,
            because: "combat initiative is stat-based and deterministic.");
    }

    [Fact]
    public void CreateFromDraft_ShouldBeDeterministic_ForSameDraft()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);

        var combat1 = factory.CreateFromDraft(draft);
        var combat2 = factory.CreateFromDraft(draft);

        combat1.Allies.Should().HaveSameCount(combat2.Allies);
        combat1.Enemies.Should().HaveSameCount(combat2.Enemies);
        combat1.TurnNumber.Should().Be(combat2.TurnNumber);
        combat1.Status.Should().Be(combat2.Status);
    }

    [Fact]
    public void CreateFromDraft_ShouldThrow_WhenDraftHasNoEnemy()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 0);

        var act = () => factory.CreateFromDraft(draft);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one enemy.");
    }

    [Fact]
    public void CreateFromDraft_ShouldThrow_WhenDraftHasNoAlly()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 0);

        var act = () => factory.CreateFromDraft(draft);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one ally.");
    }

    // -----------------------------------------------------------------------
    // StartingGuardBonus
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldApplyStartingGuardBonus_WhenModifierProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifier = RunModifier.Create(
            RunModifierType.StartingGuardBonus,
            8,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.consumable.guard-shard");

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier });

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(8,
            because: "An unconsumed StartingGuardBonus modifier with value 8 should set ally guard to 8.");
    }

    [Fact]
    public void CreateFromDraft_ShouldSumMultipleStartingGuardBonusModifiers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifiers = new[]
        {
            RunModifier.Create(RunModifierType.StartingGuardBonus, 8, RunModifierDuration.UntilRunEnds, "RunItem", "item.guard-shard.1"),
            RunModifier.Create(RunModifierType.StartingGuardBonus, 6, RunModifierDuration.UntilRunEnds, "RunItem", "item.guard-shard.2"),
        };

        var combat = factory.CreateFromDraft(draft, runModifiers: modifiers);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(14,
            because: "Two guard bonus modifiers (8 + 6) should stack to 14.");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplyConsumedStartingGuardBonusModifiers()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifier = RunModifier.Create(
            RunModifierType.StartingGuardBonus,
            8,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.consumable.guard-shard");
        modifier.Consume(DateTime.UtcNow);

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier });

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "A consumed StartingGuardBonus modifier must not contribute to starting guard.");
    }

    [Fact]
    public void CreateFromDraft_ShouldUseZeroGuard_WhenNoRunModifiersProvided()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "Without any RunModifiers the starting guard defaults to 0.");
    }

    // -----------------------------------------------------------------------
    // PalaceRoomState effects
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldNotApplyPalaceGuard_WhenNeutral()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Neutral);

        var enemy = combat.Enemies.Single();
        enemy.Guard.Should().Be(0,
            because: "Neutral PalaceRoomState should not grant any guard bonus.");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplySilentGuard_WhenSilent()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Silent);

        var enemy = combat.Enemies.Single();
        enemy.Guard.Should().Be(8,
            because: "Silent PalaceRoomState should grant 8 starting guard to enemies.");
        enemy.BaseGuard.Should().Be(8,
            because: "Silent PalaceRoomState should set base guard to 8 for round resets.");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplyAnyGuard_WhenSilent_AllySide()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Silent);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "Silent PalaceRoomState should only affect enemy guard, not allies.");
    }

    [Fact]
    public void CreateFromDraft_ShouldReduceEnemyDamageSkill_WhenPainful()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var baseline = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Neutral);
        var painful = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Painful);

        var baselinePower = baseline.Enemies.Single().Skills.Single(s => s.EffectType == "Damage").BasePower;
        var painfulPower = painful.Enemies.Single().Skills.Single(s => s.EffectType == "Damage").BasePower;

        painfulPower.Should().BeLessThan(baselinePower,
            because: "Painful PalaceRoomState should reduce enemy damage skill power by 10%.");
        painfulPower.Should().Be((int)Math.Round(baselinePower * 0.90),
            because: "Painful PalaceRoomState applies a 0.90 multiplier to enemy damage skills.");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotReduceEnemyGuardSkill_WhenPainful()
    {
        var factory = new CombatFactory();
        var guardSkill = new CombatEncounterDraftSkill(
            "skill.basic.guard", "Garde", "Guard.", "Defense", "Self", "AddCurrentGuard", 0, 0, 10,
            Array.Empty<string>());
        var draft = CreateDraft(enemySkills: [guardSkill]);

        var baseline = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Neutral);
        var painful = factory.CreateFromDraft(draft, palaceRoomState: PalaceRoomState.Painful);

        var baselinePower = baseline.Enemies.Single().Skills.Single().BasePower;
        var painfulPower = painful.Enemies.Single().Skills.Single().BasePower;

        painfulPower.Should().Be(baselinePower,
            because: "Painful PalaceRoomState should only reduce damage-type skills, not guard skills.");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyPainfulOverClimateMultiplier()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var heatwaveOnly = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 3)],
            palaceRoomState: PalaceRoomState.Neutral);
        var heatwaveAndPainful = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 3)],
            palaceRoomState: PalaceRoomState.Painful);

        var heatwavePower = heatwaveOnly.Enemies.Single().Skills.Single(s => s.EffectType == "Damage").BasePower;
        var combinedPower = heatwaveAndPainful.Enemies.Single().Skills.Single(s => s.EffectType == "Damage").BasePower;

        combinedPower.Should().BeLessThan(heatwavePower,
            because: "Painful state should compound with Heatwave climate to further reduce enemy damage.");
    }

    private static RunModifier CreateClimateModifier(Guid roomId, double value)
    {
        return RunModifier.Create(
            RunModifierType.RoomClimate,
            value,
            RunModifierDuration.UntilRoomEnds,
            "PalaceLaw",
            "law-climate-test",
            expiresAtRoomId: roomId);
    }
}
