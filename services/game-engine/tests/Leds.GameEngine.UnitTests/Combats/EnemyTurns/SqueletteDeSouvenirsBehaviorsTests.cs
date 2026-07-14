using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

/// <summary>
/// Direct <see cref="IBossBehavior.DecideAction"/> calls — see the equivalent note
/// in VeilleursDuSeuilBehaviorsTests. Several branches here are chance-gated
/// (deterministic per combat/turn/boss, but not controllable from a test without
/// a fixed seed) — those are asserted as invariants (one of N valid outcomes)
/// rather than an exact branch, mirroring GrandCardinal_ShouldNotAlwaysTarget...
/// in BossBehaviorTests.
/// </summary>
public sealed class SqueletteDeSouvenirsBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void SqueletteSouvenir_ShouldCollapse_WhenLowHealth_AndPorteurCendreAlive()
    {
        var griffe = CreateSkill("canon.skill.griffe-dos", "Damage", "SingleEnemy", 10);
        var effondrement = CreateSkill("canon.skill.effondrement", "Damage", "AllEnemies", 6);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var squelette = Combatant.CreateEnemy("canon.enemy.squelette-souvenir", "Squelette", "Skirmisher", 34, [griffe, effondrement]);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-cendre", "Porteur", "Support", 66, []);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [squelette, porteur]);
        squelette.ApplyDamage(30); // 4/34 HP ~= 12%, under 20%

        var decision = new SqueletteSouvenirBossBehavior().DecideAction(new BossDecisionContext(combat, squelette));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.effondrement");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void SqueletteSouvenir_ShouldNotCollapse_WhenLowHealth_ButNoPorteurCendreAllied()
    {
        var griffe = CreateSkill("canon.skill.griffe-dos", "Damage", "SingleEnemy", 10);
        var fragment = CreateSkill("canon.skill.fragment-grave", "Damage", "SingleEnemy", 8, "Magic");
        var etreinte = CreateSkill("canon.skill.etreinte-creuse", "Damage", "SingleEnemy", 6);
        var effondrement = CreateSkill("canon.skill.effondrement", "Damage", "AllEnemies", 6);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var squelette = Combatant.CreateEnemy(
            "canon.enemy.squelette-souvenir", "Squelette", "Skirmisher", 34, [griffe, fragment, etreinte, effondrement]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [squelette]);
        squelette.ApplyDamage(30); // under 20% HP, but alone

        var decision = new SqueletteSouvenirBossBehavior().DecideAction(new BossDecisionContext(combat, squelette));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.griffe-dos", "canon.skill.fragment-grave", "canon.skill.etreinte-creuse");
    }

    [Fact]
    public void SqueletteSouvenir_ShouldTargetLowestHpPlayer_InTheAttackCycle()
    {
        var griffe = CreateSkill("canon.skill.griffe-dos", "Damage", "SingleEnemy", 10);
        var fragment = CreateSkill("canon.skill.fragment-grave", "Damage", "SingleEnemy", 8, "Magic");
        var etreinte = CreateSkill("canon.skill.etreinte-creuse", "Damage", "SingleEnemy", 6);
        var weak = Combatant.CreateAlly("player.1", "Weak", "Fighter", 100);
        var tough = Combatant.CreateAlly("player.2", "Tough", "Fighter", 100);
        var squelette = Combatant.CreateEnemy(
            "canon.enemy.squelette-souvenir", "Squelette", "Skirmisher", 34, [griffe, fragment, etreinte]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [weak, tough], [squelette]);
        weak.ApplyDamage(60);

        var decision = new SqueletteSouvenirBossBehavior().DecideAction(new BossDecisionContext(combat, squelette));

        decision.Should().NotBeNull();
        decision!.TargetIds.Should().BeEquivalentTo(new[] { weak.Id.Value });
    }

    [Fact]
    public void PorteurCendre_ShouldHealMostWoundedAlly_WhenBelowFortyPercent()
    {
        var fardeau = CreateSkill("canon.skill.fardeau-partage", "Heal", "SingleAlly", 8, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-cendre", "Porteur", "Support", 66, [fardeau]);
        var squelette = Combatant.CreateEnemy("canon.enemy.squelette-souvenir", "Squelette", "Skirmisher", 34, []);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [porteur, squelette]);
        squelette.ApplyDamage(30); // 4/34 HP ~= 12%, under 40%

        var decision = new PorteurCendreBossBehavior().DecideAction(new BossDecisionContext(combat, porteur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.fardeau-partage");
        decision.TargetIds.Should().BeEquivalentTo(new[] { squelette.Id.Value });
    }

    [Fact]
    public void PorteurCendre_ShouldHarass_WhenNoAllyIsWounded()
    {
        var jet = CreateSkill("canon.skill.jet-de-cendre", "Damage", "SingleEnemy", 9, "Magic");
        var sursaut = CreateSkill("canon.skill.sursaut-memoriel", "Damage", "SingleEnemy", 12, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-cendre", "Porteur", "Support", 66, [jet, sursaut]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [porteur]);

        var decision = new PorteurCendreBossBehavior().DecideAction(new BossDecisionContext(combat, porteur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.jet-de-cendre", "canon.skill.sursaut-memoriel");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void ChoeurMuet_ShouldFinishSilencedTarget_Regardless()
    {
        var note = CreateSkill("canon.skill.note-tenue", "Damage", "SingleEnemy", 20, "Magic");
        var silenceSkill = CreateSkill("canon.skill.silence", "Debuff", "SingleEnemy", 0, "Magic");
        var silenced = Combatant.CreateAlly("player.1", "Silenced", "Fighter", 100);
        var other = Combatant.CreateAlly("player.2", "Other", "Fighter", 100);
        var choeur = Combatant.CreateEnemy("canon.enemy.choeur-muet", "Chœur", "Disruptor", 74, [note, silenceSkill]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [silenced, other], [choeur]);

        silenced.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.silence:Silence", "Silence", StatusEffectKind.Silence, currentTick: 0, durationTicks: 5000));

        var decision = new ChoeurMuetBossBehavior().DecideAction(new BossDecisionContext(combat, choeur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.note-tenue");
        decision.TargetIds.Should().BeEquivalentTo(new[] { silenced.Id.Value });
    }

    [Fact]
    public void ChoeurMuet_ShouldOpen_WithBerceuseOrSilence_OnFirstTurn()
    {
        var berceuse = CreateSkill("canon.skill.berceuse-inversee", "Debuff", "AllEnemies", 0, "Magic");
        var silenceSkill = CreateSkill("canon.skill.silence", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var choeur = Combatant.CreateEnemy("canon.enemy.choeur-muet", "Chœur", "Disruptor", 74, [berceuse, silenceSkill]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [choeur]);

        var decision = new ChoeurMuetBossBehavior().DecideAction(new BossDecisionContext(combat, choeur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.berceuse-inversee", "canon.skill.silence");
    }

    [Fact]
    public void ChoeurMuet_ShouldDefaultToLectureDesSilences_AfterFirstTurn_WhenNoOneIsSilenced()
    {
        var lecture = CreateSkill("canon.skill.lecture-des-silences", "Damage", "SingleEnemy", 15, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var choeur = Combatant.CreateEnemy("canon.enemy.choeur-muet", "Chœur", "Disruptor", 74, [lecture]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [choeur]);
        combat.AdvanceTurn();

        var decision = new ChoeurMuetBossBehavior().DecideAction(new BossDecisionContext(combat, choeur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.lecture-des-silences");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }
}
