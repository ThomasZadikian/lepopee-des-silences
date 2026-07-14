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
/// in VeilleursDuSeuilBehaviorsTests.
/// </summary>
public sealed class EchosDEmotionsBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void EchoColere_ShouldExplode_WhenBelowHalfHealth_AndManaAllows()
    {
        var explosion = CreateSkill("canon.skill.explosion", "Damage", "AllEnemies", 20, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-colere", "Écho", "Bruiser", 60, [explosion], mana: 16);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        echo.ApplyDamage(35); // 25/60 HP ~= 42%, under 50%

        var decision = new EchoColereBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.explosion");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EchoColere_ShouldPunishItsLastAttacker()
    {
        var constatSec = CreateSkill("canon.skill.constat-sec", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-colere", "Écho", "Bruiser", 60, [constatSec]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        echo.RecordLastAttacker(hero.Id.Value);

        var decision = new EchoColereBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.constat-sec");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EchoColere_ShouldOpenWithMontee_OnFirstTurn_WhenUnprovoked()
    {
        var montee = CreateSkill("canon.skill.montee", "Buff", "Self", 0);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-colere", "Écho", "Bruiser", 60, [montee]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);

        var decision = new EchoColereBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.montee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { echo.Id.Value });
    }

    [Fact]
    public void EchoPeur_ShouldSaccade_OnEvenTurns()
    {
        var saccade = CreateSkill("canon.skill.saccade", "Buff", "Self", 6);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-peur", "Écho", "Disruptor", 42, [saccade]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        combat.AdvanceTurn(); // turn 2 (even)

        var decision = new EchoPeurBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.saccade");
        decision.TargetIds.Should().BeEquivalentTo(new[] { echo.Id.Value });
    }

    [Fact]
    public void EchoPeur_ShouldMarkAnUndottedTarget_OnOddTurns()
    {
        var nevrose = CreateSkill("canon.skill.nevrose", "Damage", "SingleEnemy", 10, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-peur", "Écho", "Disruptor", 42, [nevrose]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        // turn 1 (odd) already

        var decision = new EchoPeurBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.nevrose");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EchoTristesse_ShouldWeighDownTheFastestPlayer_OnFirstTurn()
    {
        var poids = CreateSkill("canon.skill.poids", "Debuff", "SingleEnemy", 0, "Magic");
        var slow = Combatant.Create(
            CombatantId.New(), "player.1", "Slow", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 4);
        var fast = Combatant.Create(
            CombatantId.New(), "player.2", "Fast", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 18);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-tristesse", "Écho", "Support", 80, [poids]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [slow, fast], [echo]);

        var decision = new EchoTristesseBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.poids");
        decision.TargetIds.Should().BeEquivalentTo(new[] { fast.Id.Value });
    }

    [Fact]
    public void EchoTristesse_ShouldShareItsRespite_WithEveryoneAlive_WhenBelowFortyPercent()
    {
        var silencePartage = CreateSkill("canon.skill.silence-partage", "Heal", "AllAllies", 6, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-tristesse", "Écho", "Support", 80, [silencePartage]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        echo.ApplyDamage(50); // 30/80 HP ~= 37.5%, under 40%

        var decision = new EchoTristesseBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.silence-partage");
        decision.TargetIds.Should().BeEquivalentTo(new[] { echo.Id.Value, hero.Id.Value });
    }
}
