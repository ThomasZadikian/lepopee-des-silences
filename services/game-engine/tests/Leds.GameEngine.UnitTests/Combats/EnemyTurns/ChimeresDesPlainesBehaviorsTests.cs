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
/// in VeilleursDuSeuilBehaviorsTests. Chance-gated branches are asserted as
/// invariants rather than an exact outcome, same as CopistesBehaviorsTests /
/// SqueletteDeSouvenirsBehaviorsTests.
/// </summary>
public sealed class ChimeresDesPlainesBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category, emotionalRegister: "Neutral");
    }

    [Fact]
    public void ChimereAffamee_ShouldFinishWoundedTarget_BelowFortyPercent()
    {
        var curee = CreateSkill("canon.skill.curee", "Damage", "SingleEnemy", 16);
        var morsure = CreateSkill("canon.skill.morsure-composite", "Damage", "SingleEnemy", 13);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var chimere = Combatant.CreateEnemy("canon.enemy.chimere-affamee", "Chimère", "Skirmisher", 52, [curee, morsure]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [chimere]);
        hero.ApplyDamage(65); // 35/100 HP, under 40%

        var decision = new ChimereAffameeBossBehavior().DecideAction(new BossDecisionContext(combat, chimere));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.curee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void ChimereAffamee_ShouldTargetHeavilyDottedPlayer_OverAnUndottedOne()
    {
        var morsure = CreateSkill("canon.skill.morsure-composite", "Damage", "SingleEnemy", 13);
        var dotted = Combatant.CreateAlly("player.1", "Dotted", "Fighter", 100);
        var clean = Combatant.CreateAlly("player.2", "Clean", "Fighter", 100);
        var chimere = Combatant.CreateEnemy("canon.enemy.chimere-affamee", "Chimère", "Skirmisher", 52, [morsure]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [dotted, clean], [chimere]);

        for (var i = 0; i < 2; i++)
        {
            dotted.ApplyStatusEffect(CombatStatusEffect.Create(
                $"poison{i}", "Poison", StatusEffectKind.DamageOverTime,
                currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        }

        var decision = new ChimereAffameeBossBehavior().DecideAction(new BossDecisionContext(combat, chimere));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.morsure-composite");
        decision.TargetIds.Should().BeEquivalentTo(new[] { dotted.Id.Value });
    }

    [Fact]
    public void ChimereAffamee_ShouldWatchOrPounce_WhenNoOneIsWoundedOrDotted()
    {
        var guet = CreateSkill("canon.skill.guet", "Buff", "Self", 6);
        var bond = CreateSkill("canon.skill.bond-de-flanc", "Damage", "SingleEnemy", 10);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var chimere = Combatant.CreateEnemy("canon.enemy.chimere-affamee", "Chimère", "Skirmisher", 52, [guet, bond]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [chimere]);

        var decision = new ChimereAffameeBossBehavior().DecideAction(new BossDecisionContext(combat, chimere));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.guet", "canon.skill.bond-de-flanc");
    }

    [Fact]
    public void BergerOrdres_ShouldDesignate_OnFirstTurn()
    {
        var designation = CreateSkill("canon.skill.designation", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var berger = Combatant.CreateEnemy("canon.enemy.berger-ordres", "Berger", "Support", 70, [designation]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [berger]);

        var decision = new BergerOrdresBossBehavior().DecideAction(new BossDecisionContext(combat, berger));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.designation");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void BergerOrdres_ShouldRationTheHerd_WhenAnAllyIsWounded()
    {
        var ration = CreateSkill("canon.skill.ration", "Buff", "AllAllies", 10, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var berger = Combatant.CreateEnemy("canon.enemy.berger-ordres", "Berger", "Support", 70, [ration]);
        var chimere = Combatant.CreateEnemy("canon.enemy.chimere-affamee", "Chimère", "Skirmisher", 52, []);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [berger, chimere]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // move past the turn-1 Désignation opener
        chimere.ApplyDamage(30); // 22/52 HP ~= 42%, under 50%

        var decision = new BergerOrdresBossBehavior().DecideAction(new BossDecisionContext(combat, berger));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.ration");
        decision.TargetIds.Should().BeEquivalentTo(new[] { berger.Id.Value, chimere.Id.Value });
    }

    [Fact]
    public void BergerOrdres_ShouldCurseUndottedTarget_WhenNoAllyIsWounded()
    {
        var plongee = CreateSkill("canon.skill.plongee-dans-la-folie", "Damage", "SingleEnemy", 20, "Magic");
        var houlette = CreateSkill("canon.skill.houlette", "Damage", "SingleEnemy", 11);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var berger = Combatant.CreateEnemy("canon.enemy.berger-ordres", "Berger", "Support", 70, [plongee, houlette]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [berger]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        var decision = new BergerOrdresBossBehavior().DecideAction(new BossDecisionContext(combat, berger));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.plongee-dans-la-folie");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void AgneauInverse_ShouldDetonate_WhenLowHealth_AndMultipleOpponents()
    {
        var detente = CreateSkill("canon.skill.detente", "Damage", "AllEnemies", 26, "Magic");
        var hero1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var hero2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var agneau = Combatant.CreateEnemy("canon.enemy.agneau-inverse", "Agneau", "Disruptor", 40, [detente]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero1, hero2], [agneau]);
        agneau.ApplyDamage(32); // 8/40 HP = 20%, under 25%

        var decision = new AgneauInverseBossBehavior().DecideAction(new BossDecisionContext(combat, agneau));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.detente");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero1.Id.Value, hero2.Id.Value });
    }

    [Fact]
    public void AgneauInverse_ShouldGraze_ForTheFirstTwoTurns()
    {
        var brout = CreateSkill("canon.skill.brout", "Buff", "Self", 8);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var agneau = Combatant.CreateEnemy("canon.enemy.agneau-inverse", "Agneau", "Disruptor", 40, [brout]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [agneau]);

        var decision = new AgneauInverseBossBehavior().DecideAction(new BossDecisionContext(combat, agneau));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.brout");
        decision.TargetIds.Should().BeEquivalentTo(new[] { agneau.Id.Value });
    }

    [Fact]
    public void AgneauInverse_ShouldMarkFastestPlayer_ThenStrikeThemOnceMarked()
    {
        var regard = CreateSkill("canon.skill.regard-fixe", "Debuff", "SingleEnemy", 0, "Magic");
        var belement = CreateSkill("canon.skill.belement-a-lenvers", "Damage", "SingleEnemy", 12, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var agneau = Combatant.CreateEnemy("canon.enemy.agneau-inverse", "Agneau", "Disruptor", 40, [regard, belement]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [agneau]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 2); // turn 3, past the two grazing turns

        var firstDecision = new AgneauInverseBossBehavior().DecideAction(new BossDecisionContext(combat, agneau));
        firstDecision.Should().NotBeNull();
        firstDecision!.SkillKey.Should().Be("canon.skill.regard-fixe");

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.regard-fixe:StatModifier", "Regard fixe", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -12, stat: CombatStat.Speed));

        var secondDecision = new AgneauInverseBossBehavior().DecideAction(new BossDecisionContext(combat, agneau));
        secondDecision.Should().NotBeNull();
        secondDecision!.SkillKey.Should().Be("canon.skill.belement-a-lenvers");
    }
}
