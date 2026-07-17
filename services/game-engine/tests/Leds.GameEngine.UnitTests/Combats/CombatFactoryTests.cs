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
        int enemyMagicAttack = 0,
        int enemyMagicDefense = 0,
        int enemyMana = 0,
        double difficultyMultiplier = 1.0,
        int allyAttackPower = 0,
        int allyDefense = 0,
        int allySpeed = 10,
        int allyFocus = 0,
        int allyMagicAttack = 0,
        int allyMagicDefense = 0,
        string? roomKey = null)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            new CombatEncounterDraftAlly(
                $"player.{i}",
                $"Hero{i}",
                "Fighter",
                allyTags ?? Array.Empty<string>(),
                AttackPower: allyAttackPower,
                Defense: allyDefense,
                Speed: allySpeed,
                Focus: allyFocus,
                MagicAttack: allyMagicAttack,
                MagicDefense: allyMagicDefense)).ToArray();

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
                Focus: enemyFocus,
                MagicAttack: enemyMagicAttack,
                MagicDefense: enemyMagicDefense,
                Mana: enemyMana);
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
            DifficultyMultiplier: difficultyMultiplier,
            RoomKey: roomKey);
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
    public void CreateFromDraft_ShouldMapEnemySkillBasePowerIsPercentOfMaxVitality()
    {
        var factory = new CombatFactory();
        var percentHealSkill = new CombatEncounterDraftSkill(
            "skill.percent-heal", "Souffle vital", "Percent heal.", "Buff", "Self", "Heal", 0, 0, 25,
            Array.Empty<string>(), BasePowerIsPercentOfMaxVitality: true);
        var draft = CreateDraft(enemySkills: [percentHealSkill]);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Single().Skills.Single().BasePowerIsPercentOfMaxVitality.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldDefaultEnemySkillBasePowerIsPercentOfMaxVitalityToFalse_WhenNotSpecified()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(includeSkills: true);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Single().Skills.Single().BasePowerIsPercentOfMaxVitality.Should().BeFalse();
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
    public void CreateFromDraft_ShouldWireCatalogMagicAttackDefenseAndMana_IntoEnemyCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyMagicAttack: 12, enemyMagicDefense: 9, enemyMana: 20);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.BaseStatSnapshot.MagicAttack.Should().Be(12);
        enemy.BaseStatSnapshot.MagicDefense.Should().Be(9);
        enemy.Mana.Should().Be(20);
        enemy.MaxMana.Should().Be(20);
    }

    [Fact]
    public void CreateFromDraft_ShouldScaleEnemyMagicAttackDefense_WithDifficultyMultiplier()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyMagicAttack: 10, enemyMagicDefense: 10, difficultyMultiplier: 2.0);

        var combat = factory.CreateFromDraft(draft);

        var enemy = combat.Enemies.Single();
        enemy.BaseStatSnapshot.MagicAttack.Should().Be(20,
            because: "MagicAttack should keep pace with run depth, same as the other authored stats.");
        enemy.BaseStatSnapshot.MagicDefense.Should().Be(20,
            because: "MagicDefense should keep pace with run depth, same as the other authored stats.");
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
    public void CreateFromDraft_ShouldApplyGuardBonusPercent_OnTopOfStartingGuardBonus()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var modifier = RunModifier.Create(
            RunModifierType.StartingGuardBonus,
            100,
            RunModifierDuration.UntilRunEnds,
            "RunItem",
            "item.consumable.guard-shard");

        var combat = factory.CreateFromDraft(draft, runModifiers: new[] { modifier }, guardBonusPercent: 20);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(120,
            because: "Bague de Iris: +20% of whatever starting guard the run would otherwise grant (100 -> 120).");
    }

    [Fact]
    public void CreateFromDraft_ShouldLeaveGuardAtZero_WhenGuardBonusPercentAppliesToNoBaseGuard()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft, guardBonusPercent: 20);

        var ally = combat.Allies.Single();
        ally.Guard.Should().Be(0,
            because: "20% of zero starting guard is still zero.");
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

    // ---------------------------------------------------------------------------
    // Chapitre II — the 4 new SFD climates (Brume/Orage/Pluie de cendres/Pluie
    // violacée), additive alongside the legacy Grey/Rain/Heatwave/Hail values.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldApplyBrumeClimateFocusPenalty_ToEveryCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            focus: 10,
            enemyFocus: 10,
            runModifiers: [CreateClimateModifier(draft.RoomId, 5)]);

        combat.Allies.Single().EffectiveFocus.Should().Be(7,
            because: "Loi du Voile / Brume applies a flat -3 Focus penalty to every combatant.");
        combat.Enemies.Single().EffectiveFocus.Should().Be(7,
            because: "The Brume Focus penalty applies to enemies too.");
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyOrageClimateMagicDamageBonus_ToEveryCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 6)]);

        combat.Allies.Single().EffectiveMagicDamageBonusPercent.Should().Be(15,
            because: "Loi des Accords / Orage grants +15% magic damage to every combatant.");
        combat.Enemies.Single().EffectiveMagicDamageBonusPercent.Should().Be(15);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyPluieDeCendresClimateHealingAndDotBonus_ToEveryCombatant()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 7)]);

        var ally = combat.Allies.Single();
        ally.EffectiveHealingBonusPercent.Should().Be(-25,
            because: "Loi du Deuil Sec / Pluie de cendres reduces healing by 25%.");
        ally.EffectiveDotDamageBonusPercent.Should().Be(15,
            because: "\"+15% dégâts de feu\" is reinterpreted as +15% DoT damage bonus (no elemental fire type exists).");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplyAnyClimateBundle_WhenPluieViolaceeIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(
            draft,
            runModifiers: [CreateClimateModifier(draft.RoomId, 8)]);

        var ally = combat.Allies.Single();
        ally.EffectiveMagicDamageBonusPercent.Should().Be(0);
        ally.EffectiveHealingBonusPercent.Should().Be(0);
        ally.EffectiveDotDamageBonusPercent.Should().Be(0);
        combat.DotMagnitudeBonus.Should().Be(1,
            because: "Loi de la Marée Haute / Pluie violacée is threaded through Combat.DotMagnitudeBonus instead of a stat bundle.");
    }

    [Fact]
    public void CreateFromDraft_ShouldLeaveDotMagnitudeBonusAtZero_WhenNoClimateIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.DotMagnitudeBonus.Should().Be(0);
    }

    // ---------------------------------------------------------------------------
    // "Édit du Souvenir Doux" (AllyHealingBonus) — allies only, unlike the climate
    // bundles above which apply to both sides.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldApplyAllyHealingBonus_WhenTheEditIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.AllyHealingBonus, 20, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-souvenir-doux-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.Allies.Single().EffectiveHealingBonusPercent.Should().Be(20);
        combat.Enemies.Single().EffectiveHealingBonusPercent.Should().Be(0,
            because: "Édit du Souvenir Doux only boosts healing received by the player's own team.");
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplyAllyHealingBonus_WhenTheEditIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Allies.Single().EffectiveHealingBonusPercent.Should().Be(0);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Silence Dû" (SilenceDuActive) — symmetric, both sides.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldApplySilenceDuBundle_ToEveryCombatant_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.SilenceDuActive, 1, RunModifierDuration.UntilRoomEnds, "PalaceLaw", "law-silence-du-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.Allies.Single().EffectivePhysicalDamageBonusPercent.Should().Be(8);
        combat.Allies.Single().EffectiveFlatManaCostBonus.Should().Be(2);
        combat.Enemies.Single().EffectivePhysicalDamageBonusPercent.Should().Be(8);
        combat.Enemies.Single().EffectiveFlatManaCostBonus.Should().Be(2);
    }

    [Fact]
    public void CreateFromDraft_ShouldNotApplySilenceDuBundle_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Allies.Single().EffectivePhysicalDamageBonusPercent.Should().Be(0);
        combat.Allies.Single().EffectiveFlatManaCostBonus.Should().Be(0);
    }

    // ---------------------------------------------------------------------------
    // "Loi de l'Éloge Funèbre" (PostDeathBasicAttackOnlyEnabled) — RunModifier-driven,
    // baked at combat creation; the actual post-death gate lives on Combat itself
    // (RegisterCombatantDefeated/NextActionRestrictedToBasicAttack), tested in
    // CombatSkillActionValidatorTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldEnablePostDeathBasicAttackOnly_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.PostDeathBasicAttackOnly, 1, RunModifierDuration.UntilRoomEnds, "PalaceLaw", "law-eloge-funebre-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.PostDeathBasicAttackOnlyEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotEnablePostDeathBasicAttackOnly_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.PostDeathBasicAttackOnlyEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi du Tapis Propre" (TapisPropreEnabled) — RunModifier-driven, baked at
    // combat creation; the per-combatant first-turn gate itself lives on Combatant
    // (HasActedThisCombat), tested in CombatSkillActionValidatorTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldEnableTapisPropre_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.TapisPropreEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-tapis-propre-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.TapisPropreEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotEnableTapisPropre_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.TapisPropreEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Troisième Tasse" (ThirdCupHealCorruptionEnabled) — RunModifier-driven,
    // baked at combat creation; the per-application roll itself lives on Combat
    // (ApplyThirdCupRollIfActive), tested in CombatSkillEffectResolverTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldEnableThirdCupHealCorruption_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.ThirdCupHealCorruptionEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-troisieme-tasse-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.ThirdCupHealCorruptionEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotEnableThirdCupHealCorruption_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.ThirdCupHealCorruptionEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi des Présentations" (PresentationsEnabled) — RunModifier-driven, baked at
    // combat creation; the per-enemy first-action forecast itself lives in
    // EnemyCombatTurnResolver.Resolve, tested in EnemyCombatTurnResolverTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldEnablePresentations_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.PresentationsEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-presentations-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.PresentationsEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotEnablePresentations_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.PresentationsEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi du Miroir" (MiroirEnabled) — RunModifier-driven, baked at combat creation;
    // the mirror-copy mechanic itself lives in
    // UseCombatSkillCommandHandler.ResolveMirrorCopyIfTriggered, tested there.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldEnableMiroir_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();
        var modifier = RunModifier.Create(
            RunModifierType.MiroirEnabled, 1, RunModifierDuration.UntilFloorEnds, "PalaceLaw", "law-miroir-test");

        var combat = factory.CreateFromDraft(draft, runModifiers: [modifier]);

        combat.MiroirEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotEnableMiroir_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.MiroirEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi des Visites Terminées" (HealingBlocked) — room-bound (RoomKey), not a
    // RunModifier/tirage.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldSetHealingBlocked_WhenRoomKeyIsHopital()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(roomKey: "room.hopital");

        var combat = factory.CreateFromDraft(draft);

        combat.HealingBlocked.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotSetHealingBlocked_WhenRoomKeyIsSomethingElse()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(roomKey: "room.jardin");

        var combat = factory.CreateFromDraft(draft);

        combat.HealingBlocked.Should().BeFalse();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotSetHealingBlocked_WhenRoomKeyIsAbsent()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.HealingBlocked.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Falaise" (FalaiseWindEnabled) — room-bound (RoomKey), same
    // convention as HealingBlocked above.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldSetFalaiseWindEnabled_WhenRoomKeyIsFalaise()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(roomKey: "room.falaise");

        var combat = factory.CreateFromDraft(draft);

        combat.FalaiseWindEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotSetFalaiseWindEnabled_WhenRoomKeyIsSomethingElse()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(roomKey: "room.jardin");

        var combat = factory.CreateFromDraft(draft);

        combat.FalaiseWindEnabled.Should().BeFalse();
    }

    [Fact]
    public void CreateFromDraft_ShouldNotSetFalaiseWindEnabled_WhenRoomKeyIsAbsent()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.FalaiseWindEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi du Reflet" (MirrorCombatCopy) — mirrors the PLAYER's own team into the
    // enemy slot for the next combat (60% stats, same skills), replacing whatever
    // enemies the room would have spawned — not a clone of those enemies.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldNotReplaceEnemies_WhenMirrorCombatCopyIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);

        var combat = factory.CreateFromDraft(draft);

        combat.Enemies.Should().HaveCount(2);
        combat.Enemies.Should().OnlyContain(e => !e.DisplayName.StartsWith("Reflet"));
    }

    [Fact]
    public void CreateFromDraft_ShouldReplaceEnemiesWithMirroredAllies_WhenMirrorCombatCopyIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 2, enemyCount: 3);

        var combat = factory.CreateFromDraft(draft, runModifiers: [CreateMirrorCombatCopyModifier()]);

        // The mirrored enemy side matches the ALLY roster, not the room's own enemies.
        combat.Enemies.Should().HaveCount(2);
        combat.Enemies.Should().OnlyContain(e => e.DisplayName.StartsWith("Reflet de"));
    }

    [Fact]
    public void CreateFromDraft_ShouldScaleTheMirroredAlly_ToSixtyPercentStats()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 1, allyAttackPower: 20, allyDefense: 10, allySpeed: 20);

        var combat = factory.CreateFromDraft(draft, runModifiers: [CreateMirrorCombatCopyModifier()]);

        var ally = combat.Allies.Single();
        var mirror = combat.Enemies.Single();

        mirror.MaxVitality.Should().Be((int)Math.Round(ally.MaxVitality * 0.6));
        mirror.BaseStatSnapshot.AttackPower.Should().Be((int)Math.Round(ally.BaseStatSnapshot.AttackPower * 0.6));
        mirror.BaseStatSnapshot.Defense.Should().Be((int)Math.Round(ally.BaseStatSnapshot.Defense * 0.6));
        mirror.BaseStatSnapshot.Speed.Should().Be((int)Math.Round(ally.BaseStatSnapshot.Speed * 0.6));
    }

    [Fact]
    public void CreateFromDraft_ShouldGiveTheMirroredAlly_TheSameSkillRotation()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(allyCount: 1);

        var combat = factory.CreateFromDraft(draft, runModifiers: [CreateMirrorCombatCopyModifier()]);

        var ally = combat.Allies.Single();
        var mirror = combat.Enemies.Single();

        mirror.Skills.Should().HaveSameCount(ally.Skills);
        mirror.Skills.Select(s => s.Key).Should().BeEquivalentTo(ally.Skills.Select(s => s.Key));
    }

    private static RunModifier CreateMirrorCombatCopyModifier()
    {
        return RunModifier.Create(
            RunModifierType.MirrorCombatCopy,
            1,
            RunModifierDuration.UntilRunEnds,
            "PalaceLaw",
            "law-reflet-test");
    }

    // ---------------------------------------------------------------------------
    // "Loi du Sablier Renversé" (TurnOrderReverse)
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldNotAlterSpeed_WhenTurnOrderReverseIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemySpeed: 30);

        var combat = factory.CreateFromDraft(draft, speed: 10);

        combat.Allies.Single().EffectiveSpeed.Should().Be(10);
        combat.Enemies.Single().EffectiveSpeed.Should().Be(30);
    }

    [Fact]
    public void CreateFromDraft_ShouldMirrorSpeedAroundTheRosterRange_WhenTurnOrderReverseIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemySpeed: 30);

        var combat = factory.CreateFromDraft(
            draft, speed: 10, runModifiers: [CreateTurnOrderReverseModifier()]);

        // min=10, max=30 => slowest (ally, 10) and fastest (enemy, 30) swap places.
        combat.Allies.Single().EffectiveSpeed.Should().Be(30);
        combat.Enemies.Single().EffectiveSpeed.Should().Be(10);
    }

    [Fact]
    public void CreateFromDraft_ShouldLeaveTheMedianCombatantUnchanged_WhenTurnOrderReverseIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyCount: 2);
        // Ally speed 10, one enemy at 30 (fastest), one enemy exactly at the midpoint (20).
        var midpointDraft = draft with
        {
            Enemies = new[]
            {
                draft.Enemies.ElementAt(0) with { Speed = 30 },
                draft.Enemies.ElementAt(1) with { Speed = 20 }
            }
        };

        var combat = factory.CreateFromDraft(
            midpointDraft, speed: 10, runModifiers: [CreateTurnOrderReverseModifier()]);

        // min=10, max=30 => midpoint (20) mirrors to itself: 10+30-20=20.
        combat.Enemies.Single(e => e.SourceKey == "enemy.1").EffectiveSpeed.Should().Be(20);
    }

    private static RunModifier CreateTurnOrderReverseModifier()
    {
        return RunModifier.Create(
            RunModifierType.TurnOrderReverse,
            1,
            RunModifierDuration.UntilRoomEnds,
            "PalaceLaw",
            "law-sablier-test");
    }

    // ---------------------------------------------------------------------------
    // "Loi de la File Indienne" (TurnOrderLock) — approximated by flattening every
    // combatant's Speed to the roster average (documented simplification, see
    // CombatFactory.ApplyStrictInitiativeOrder).
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldNotAlterSpeed_WhenTurnOrderLockIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemySpeed: 30);

        var combat = factory.CreateFromDraft(draft, speed: 10);

        combat.Allies.Single().EffectiveSpeed.Should().Be(10);
        combat.Enemies.Single().EffectiveSpeed.Should().Be(30);
    }

    [Fact]
    public void CreateFromDraft_ShouldFlattenSpeedToTheRosterAverage_WhenTurnOrderLockIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemySpeed: 30);

        var combat = factory.CreateFromDraft(
            draft, speed: 10, runModifiers: [CreateTurnOrderLockModifier()]);

        // average(10, 30) = 20 for both.
        combat.Allies.Single().EffectiveSpeed.Should().Be(20);
        combat.Enemies.Single().EffectiveSpeed.Should().Be(20);
    }

    private static RunModifier CreateTurnOrderLockModifier()
    {
        return RunModifier.Create(
            RunModifierType.TurnOrderLock,
            1,
            RunModifierDuration.UntilRoomEnds,
            "PalaceLaw",
            "law-file-indienne-test");
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Destinée" (CruelDestinyForEveryone) — grants "Une destinée cruelle"
    // (+20% Attack/Defense/Speed/Focus, -15% ATB tempo, 10%-max-HP eternal DoT) to
    // every combatant, both sides.
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateFromDraft_ShouldGrantCruelDestinyToEveryone_WhenTheLawIsActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft(enemyAttackPower: 100, enemyDefense: 100, enemySpeed: 100, enemyFocus: 100);

        var combat = factory.CreateFromDraft(
            draft, attackPower: 100, defense: 100, speed: 100, focus: 100,
            runModifiers: [CreateCruelDestinyModifier()]);

        var ally = combat.Allies.Single();
        var enemy = combat.Enemies.Single();

        ally.EffectiveAttackPower.Should().Be(120);
        ally.EffectiveDefense.Should().Be(120);
        ally.EffectiveSpeed.Should().Be(120);
        ally.EffectiveFocus.Should().Be(120);
        ally.EffectiveAtbTempoModifierPercent.Should().Be(-15);
        ally.StatusEffects.Should().Contain(e => e.Key == "law-destinee:dot" && e.Magnitude == 10);

        enemy.EffectiveAttackPower.Should().Be(120);
        enemy.EffectiveAtbTempoModifierPercent.Should().Be(-15);
        enemy.StatusEffects.Should().Contain(e => e.Key == "law-destinee:dot" && e.Magnitude == 10);
    }

    [Fact]
    public void CreateFromDraft_ShouldNotGrantCruelDestiny_WhenTheLawIsNotActive()
    {
        var factory = new CombatFactory();
        var draft = CreateDraft();

        var combat = factory.CreateFromDraft(draft);

        combat.Allies.Single().StatusEffects.Should().NotContain(e => e.Key == "law-destinee:dot");
        combat.Enemies.Single().StatusEffects.Should().NotContain(e => e.Key == "law-destinee:dot");
    }

    private static RunModifier CreateCruelDestinyModifier()
    {
        return RunModifier.Create(
            RunModifierType.CruelDestinyForEveryone,
            1,
            RunModifierDuration.UntilRoomEnds,
            "PalaceLaw",
            "law-destinee-test");
    }
}
