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
/// These tests call <see cref="IBossBehavior.DecideAction"/> directly instead of
/// going through <see cref="Leds.GameEngine.Application.Combats.EnemyTurns.EnemyCombatTurnResolver"/>,
/// so results don't depend on which combatant the ATB scheduler happens to elect
/// active — only on the behavior's own deterministic decision logic.
/// </summary>
public sealed class VeilleursDuSeuilBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category, emotionalRegister: "Neutral");
    }

    [Fact]
    public void VeilleurTapis_ShouldCastRempartOnAllAllies_OnFirstTurnWithAnotherGuardAlive()
    {
        var rempart = CreateSkill("canon.skill.rempart", "Buff", "AllAllies", 7);
        var tapis = CreateSkill("canon.skill.pli-du-tapis", "Damage", "SingleEnemy", 9);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var veilleur1 = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur 1", "Guard", 62, [rempart, tapis]);
        var veilleur2 = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur 2", "Guard", 62, [rempart, tapis]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [veilleur1, veilleur2]);

        var decision = new VeilleurTapisBossBehavior().DecideAction(new BossDecisionContext(combat, veilleur1));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.rempart");
        decision.TargetIds.Should().BeEquivalentTo(new[] { veilleur1.Id.Value, veilleur2.Id.Value });
    }

    [Fact]
    public void VeilleurTapis_ShouldHarrySoleTarget_WhenLastGuardStanding()
    {
        var tapis = CreateSkill("canon.skill.pli-du-tapis", "Damage", "SingleEnemy", 9);
        var etouffement = CreateSkill("canon.skill.etouffement-feutre", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var veilleur = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur", "Guard", 62, [tapis, etouffement]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [veilleur]);
        // TurnNumber starts at 1 (odd) → alternation picks Pli du tapis.

        var decision = new VeilleurTapisBossBehavior().DecideAction(new BossDecisionContext(combat, veilleur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.pli-du-tapis");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void VeilleurTapis_ShouldPunishLowestHpPlayer_WhenNotFirstTurnAndGuardsRemain()
    {
        var tapis = CreateSkill("canon.skill.pli-du-tapis", "Damage", "SingleEnemy", 9);
        var seuil = CreateSkill("canon.skill.seuil-souille", "Damage", "SingleEnemy", 14, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var veilleur1 = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur 1", "Guard", 62, [tapis, seuil]);
        var veilleur2 = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur 2", "Guard", 62, []);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [veilleur1, veilleur2]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        var decision = new VeilleurTapisBossBehavior().DecideAction(new BossDecisionContext(combat, veilleur1));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.seuil-souille");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void PorteurPlateau_ShouldHealMostWoundedAlly_WhenBelowSixtyPercent()
    {
        var service = CreateSkill("canon.skill.service-du-the", "Heal", "SingleAlly", 12, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-plateau", "Porteur", "Support", 44, [service]);
        var veilleur = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur", "Guard", 60, []);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [porteur, veilleur]);
        veilleur.ApplyDamage(50); // 10/60 ~= 16.7% HP, well under 60%

        var decision = new PorteurPlateauBossBehavior().DecideAction(new BossDecisionContext(combat, porteur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.service-du-the");
        decision.TargetIds.Should().BeEquivalentTo(new[] { veilleur.Id.Value });
    }

    [Fact]
    public void PorteurPlateau_ShouldBuffTeam_OnFirstTurnWithNoWoundedAlly()
    {
        var etiquette = CreateSkill("canon.skill.etiquette", "Buff", "AllAllies", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var porteur = Combatant.CreateEnemy("canon.enemy.porteur-plateau", "Porteur", "Support", 44, [etiquette]);
        var veilleur = Combatant.CreateEnemy("canon.enemy.veilleur-tapis", "Veilleur", "Guard", 62, []);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [porteur, veilleur]);

        var decision = new PorteurPlateauBossBehavior().DecideAction(new BossDecisionContext(combat, porteur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.etiquette");
        decision.TargetIds.Should().BeEquivalentTo(new[] { porteur.Id.Value, veilleur.Id.Value });
    }

    [Fact]
    public void EchoPolitesse_ShouldSpreadBrumeOnAllPlayers_OnFirstTurn()
    {
        var brume = CreateSkill("canon.skill.brume", "Debuff", "AllEnemies", 0, "Magic");
        var hero1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var hero2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-politesse", "Écho", "Disruptor", 38, [brume]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero1, hero2], [echo]);

        var decision = new EchoPolitesseBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.brume");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero1.Id.Value, hero2.Id.Value });
    }

    [Fact]
    public void EchoPolitesse_ShouldRetreat_WhenBelowHalfHealth()
    {
        var courbette = CreateSkill("canon.skill.courbette-inversee", "Buff", "Self", 0, "Magic");
        var formule = CreateSkill("canon.skill.formule-creuse", "Damage", "SingleEnemy", 13, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-politesse", "Écho", "Disruptor", 38, [courbette, formule]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // move past the turn-1 special case
        echo.ApplyDamage(30); // 8/38 HP ~= 21%, under 50%

        var decision = new EchoPolitesseBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.courbette-inversee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { echo.Id.Value });
    }

    [Fact]
    public void EchoPolitesse_ShouldHarass_WhenNotFirstTurnAndHealthy()
    {
        var formule = CreateSkill("canon.skill.formule-creuse", "Damage", "SingleEnemy", 13, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var echo = Combatant.CreateEnemy("canon.enemy.echo-politesse", "Écho", "Disruptor", 38, [formule]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [echo]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        var decision = new EchoPolitesseBossBehavior().DecideAction(new BossDecisionContext(combat, echo));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.formule-creuse");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void SentinelleSeuil_ShouldExecuteJudgedTarget_WhenDefenseAndFocusAreBothDebuffed()
    {
        var verdict = CreateSkill("canon.skill.verdict-du-seuil", "Damage", "SingleEnemy", 28, "Magic");
        var chute = CreateSkill("canon.skill.chute-de-marbre", "Damage", "SingleEnemy", 18);
        var hero1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var hero2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var sentinelle = Combatant.CreateEnemy("canon.enemy.sentinelle-seuil", "Sentinelle", "Bruiser", 88, [verdict, chute]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero1, hero2], [sentinelle]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // move past the turn-1 special case

        hero1.ApplyStatusEffect(CombatStatusEffect.Create(
            "debuff.defense", "Défense entamée", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 10_000, magnitude: -4, stat: CombatStat.Defense));
        hero1.ApplyStatusEffect(CombatStatusEffect.Create(
            "debuff.focus", "Focus entamé", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 10_000, magnitude: -2, stat: CombatStat.Focus));

        var decision = new SentinelleSeuilBossBehavior().DecideAction(new BossDecisionContext(combat, sentinelle));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.verdict-du-seuil");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero1.Id.Value });
    }

    [Fact]
    public void SentinelleSeuil_ShouldNotExecute_WhenOnlyOneOfDefenseOrFocusIsDebuffed()
    {
        var verdict = CreateSkill("canon.skill.verdict-du-seuil", "Damage", "SingleEnemy", 28, "Magic");
        var chute = CreateSkill("canon.skill.chute-de-marbre", "Damage", "SingleEnemy", 18);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var sentinelle = Combatant.CreateEnemy("canon.enemy.sentinelle-seuil", "Sentinelle", "Bruiser", 88, [verdict, chute]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [sentinelle]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "debuff.defense", "Défense entamée", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 10_000, magnitude: -4, stat: CombatStat.Defense));

        var decision = new SentinelleSeuilBossBehavior().DecideAction(new BossDecisionContext(combat, sentinelle));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.chute-de-marbre");
    }

    [Fact]
    public void SentinelleSeuil_ShouldTakePosition_OnFirstTurn_WhenNoTargetIsJudged()
    {
        var socle = CreateSkill("canon.skill.socle", "Buff", "Self", 15);
        var chute = CreateSkill("canon.skill.chute-de-marbre", "Damage", "SingleEnemy", 18);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var sentinelle = Combatant.CreateEnemy("canon.enemy.sentinelle-seuil", "Sentinelle", "Bruiser", 88, [socle, chute]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [sentinelle]);

        var decision = new SentinelleSeuilBossBehavior().DecideAction(new BossDecisionContext(combat, sentinelle));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.socle");
        decision.TargetIds.Should().BeEquivalentTo(new[] { sentinelle.Id.Value });
    }
}
