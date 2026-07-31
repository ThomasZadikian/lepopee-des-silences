using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

/// <summary>
/// Direct <see cref="IBossBehavior.DecideAction"/> calls — see the equivalent note
/// in VeilleursDuSeuilBehaviorsTests.
/// </summary>
public sealed class PenitentsDeLaMontagneBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void PelerinSansVisage_ShouldPray_OnTheHighestHpPlayer_DuringTheOpeningTurns()
    {
        var priere = CreateSkill("canon.skill.priere-aspiration", "Drain", "SingleEnemy", 12, "Magic");
        var weak = Combatant.CreateAlly("player.1", "Weak", "Fighter", 40);
        var strong = Combatant.CreateAlly("player.2", "Strong", "Fighter", 100);
        var pelerin = Combatant.CreateEnemy("canon.enemy.pelerin-sans-visage", "Pèlerin", "Skirmisher", 42, [priere]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [weak, strong], [pelerin]);

        var decision = new PelerinSansVisageBossBehavior().DecideAction(new BossDecisionContext(combat, pelerin));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.priere-aspiration");
        decision.TargetIds.Should().BeEquivalentTo(new[] { strong.Id.Value });
    }

    [Fact]
    public void PelerinSansVisage_ShouldFlagellateItself_WhenBelowHalfHealth()
    {
        var repentir = CreateSkill("canon.skill.repentir", "Buff", "Self", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var pelerin = Combatant.CreateEnemy("canon.enemy.pelerin-sans-visage", "Pèlerin", "Skirmisher", 42, [repentir]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [pelerin]);
        pelerin.ApplyDamage(25); // 17/42 HP ~= 40%, under 50%

        var decision = new PelerinSansVisageBossBehavior().DecideAction(new BossDecisionContext(combat, pelerin));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.repentir");
        decision.TargetIds.Should().BeEquivalentTo(new[] { pelerin.Id.Value });
    }

    [Fact]
    public void PelerinSansVisage_ShouldTargetTheSoftestMagicDefense_AfterTheOpening()
    {
        var chapelet = CreateSkill("canon.skill.chapelet-de-dents", "Damage", "SingleEnemy", 11, "Magic");
        var soft = Combatant.Create(
            CombatantId.New(), "player.1", "Soft", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 2);
        var tough = Combatant.Create(
            CombatantId.New(), "player.2", "Tough", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 20);
        var pelerin = Combatant.CreateEnemy("canon.enemy.pelerin-sans-visage", "Pèlerin", "Skirmisher", 42, [chapelet]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [soft, tough], [pelerin]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 3); // turn 4, past the two opening turns

        var decision = new PelerinSansVisageBossBehavior().DecideAction(new BossDecisionContext(combat, pelerin));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.chapelet-de-dents");
        decision.TargetIds.Should().BeEquivalentTo(new[] { soft.Id.Value });
    }

    [Fact]
    public void PrieurLituique_ShouldOpenWithEncensInverse_OnFirstTurn()
    {
        var encens = CreateSkill("canon.skill.encens-inverse", "Debuff", "AllEnemies", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var prieur = Combatant.CreateEnemy("canon.enemy.prieur-lituique", "Prieur", "Support", 72, [encens]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [prieur]);

        var decision = new PrieurLituiqueBossBehavior().DecideAction(new BossDecisionContext(combat, prieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.encens-inverse");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void PrieurLituique_ShouldDrainTheWeakenedTarget()
    {
        var oraison = CreateSkill("canon.skill.oraison-cousue", "Damage", "SingleEnemy", 18, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var prieur = Combatant.CreateEnemy("canon.enemy.prieur-lituique", "Prieur", "Support", 72, [oraison]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [prieur]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // move past the turn-1 opener

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "debuff.defense", "Défense entamée", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -4, stat: CombatStat.Defense));

        var decision = new PrieurLituiqueBossBehavior().DecideAction(new BossDecisionContext(combat, prieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.oraison-cousue");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void PrieurLituique_ShouldPressTheRhythm_WhenTheFightDragsOn()
    {
        var derniere = CreateSkill("canon.skill.derniere-priere", "Drain", "SingleEnemy", 18, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var prieur = Combatant.CreateEnemy("canon.enemy.prieur-lituique", "Prieur", "Support", 72, [derniere]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [prieur]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 6);

        var decision = new PrieurLituiqueBossBehavior().DecideAction(new BossDecisionContext(combat, prieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.derniere-priere");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void FrayeurExhumee_ShouldMarkTheMostFocusedPlayer_OnArrival()
    {
        var posture = CreateSkill("canon.skill.posture-finale", "Debuff", "SingleEnemy", 0, "Magic");
        var dull = Combatant.Create(
            CombatantId.New(), "player.1", "Dull", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            focus: 2);
        var sharp = Combatant.Create(
            CombatantId.New(), "player.2", "Sharp", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            focus: 15);
        var frayeur = Combatant.CreateEnemy("canon.enemy.frayeur-exhumee", "Frayeur", "Bruiser", 104, [posture]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [dull, sharp], [frayeur]);

        var decision = new FrayeurExhumeeBossBehavior().DecideAction(new BossDecisionContext(combat, frayeur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.posture-finale");
        decision.TargetIds.Should().BeEquivalentTo(new[] { sharp.Id.Value });
    }

    [Fact]
    public void FrayeurExhumee_ShouldMarkAnUndottedTarget_AfterArrival()
    {
        var nevrose = CreateSkill("canon.skill.nevrose", "Damage", "SingleEnemy", 10, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var frayeur = Combatant.CreateEnemy("canon.enemy.frayeur-exhumee", "Frayeur", "Bruiser", 104, [nevrose]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [frayeur]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        var decision = new FrayeurExhumeeBossBehavior().DecideAction(new BossDecisionContext(combat, frayeur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.nevrose");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }
}
