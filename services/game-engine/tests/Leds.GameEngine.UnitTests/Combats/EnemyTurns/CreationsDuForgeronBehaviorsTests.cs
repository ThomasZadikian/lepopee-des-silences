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
/// in VeilleursDuSeuilBehaviorsTests. Chance-gated branches are asserted as
/// invariants rather than an exact outcome.
/// </summary>
public sealed class CreationsDuForgeronBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void CreationInstable_ShouldPunishItsLastAttacker()
    {
        var foyer = CreateSkill("canon.skill.foyer-ouvert", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var creation = Combatant.CreateEnemy("canon.enemy.creation-instable", "Création", "Bruiser", 78, [foyer]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [creation]);
        creation.RecordLastAttacker(hero.Id.Value);

        var decision = new CreationInstableBossBehavior().DecideAction(new BossDecisionContext(combat, creation));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.foyer-ouvert");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void CreationInstable_ShouldCycleAttackOrRecover_WhenUnprovoked_AndHealthy()
    {
        var coup = CreateSkill("canon.skill.coup-de-plaque", "Damage", "SingleEnemy", 12);
        var redressement = CreateSkill("canon.skill.redressement", "Buff", "Self", 0, "Physical");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var creation = Combatant.CreateEnemy("canon.enemy.creation-instable", "Création", "Bruiser", 78, [coup, redressement]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [creation]);

        var decision = new CreationInstableBossBehavior().DecideAction(new BossDecisionContext(combat, creation));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.coup-de-plaque", "canon.skill.redressement");
    }

    [Fact]
    public void MarteauVivant_ShouldUnleashSouffle_WhenAttackBuffed()
    {
        var souffle = CreateSkill("canon.skill.souffle-de-la-forge", "Damage", "SingleEnemy", 10, "Physical");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var marteau = Combatant.CreateEnemy("canon.enemy.marteau-vivant", "Marteau", "Bruiser", 64, [souffle]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [marteau]);

        marteau.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.transmutation-alliee:attack", "Transmutation", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: 4, stat: CombatStat.AttackPower));

        var decision = new MarteauVivantBossBehavior().DecideAction(new BossDecisionContext(combat, marteau));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.souffle-de-la-forge");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void MarteauVivant_ShouldFinishDottedTarget_WhenNotBuffed()
    {
        var coupdegrace = CreateSkill("canon.skill.coup-de-grace-forgeron", "Damage", "SingleEnemy", 22);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var marteau = Combatant.CreateEnemy("canon.enemy.marteau-vivant", "Marteau", "Bruiser", 64, [coupdegrace]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [marteau]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "burn", "Brûlure", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));

        var decision = new MarteauVivantBossBehavior().DecideAction(new BossDecisionContext(combat, marteau));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.coup-de-grace-forgeron");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void MarteauVivant_ShouldOpenWithCadence_OnFirstTurn_WhenUnbuffedAndNoDot()
    {
        var cadence = CreateSkill("canon.skill.cadence", "Buff", "Self", 0);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var marteau = Combatant.CreateEnemy("canon.enemy.marteau-vivant", "Marteau", "Bruiser", 64, [cadence]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [marteau]);

        var decision = new MarteauVivantBossBehavior().DecideAction(new BossDecisionContext(combat, marteau));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.cadence");
        decision.TargetIds.Should().BeEquivalentTo(new[] { marteau.Id.Value });
    }

    [Fact]
    public void SentinelleFonte_ShouldActivateMarteauVivant_OnFirstTurn()
    {
        var transmutation = CreateSkill("canon.skill.transmutation-alliee", "Buff", "SingleAlly", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var sentinelle = Combatant.CreateEnemy("canon.enemy.sentinelle-fonte", "Sentinelle", "Support", 82, [transmutation]);
        var marteau = Combatant.CreateEnemy("canon.enemy.marteau-vivant", "Marteau", "Bruiser", 64, []);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [sentinelle, marteau]);

        var decision = new SentinelleFonteBossBehavior().DecideAction(new BossDecisionContext(combat, sentinelle));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.transmutation-alliee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { marteau.Id.Value });
    }

    [Fact]
    public void SentinelleFonte_ShouldWorkTheLeastDefendedTarget_ByDefault()
    {
        var scorie = CreateSkill("canon.skill.scorie", "Damage", "SingleEnemy", 11, "Magic");
        var sturdy = Combatant.Create(
            CombatantId.New(), "player.1", "Sturdy", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 20);
        var frail = Combatant.Create(
            CombatantId.New(), "player.2", "Frail", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 2);
        var sentinelle = Combatant.CreateEnemy("canon.enemy.sentinelle-fonte", "Sentinelle", "Support", 82, [scorie]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [sturdy, frail], [sentinelle]);
        combat.AdvanceTurn(); // move past the turn-1 opener (no Marteau Vivant present anyway)

        var decision = new SentinelleFonteBossBehavior().DecideAction(new BossDecisionContext(combat, sentinelle));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.scorie");
        decision.TargetIds.Should().BeEquivalentTo(new[] { frail.Id.Value });
    }

    [Fact]
    public void ScorieRampante_ShouldAlwaysTargetTheOnlyLivingPlayer_WhenOneIsDotted()
    {
        // Whichever branch fires (Laitier ardent is chance-gated at 75%, the
        // fallback cycle is chance-gated too), the sole living player is the only
        // possible target — so this is deterministic on TargetIds even though the
        // exact SkillKey isn't, without needing to control the underlying roll.
        var laitier = CreateSkill("canon.skill.laitier-ardent", "Debuff", "SingleEnemy", 40, "Magic");
        var eclat = CreateSkill("canon.skill.eclat-vitrifie", "Damage", "SingleEnemy", 9);
        var contact = CreateSkill("canon.skill.contact", "Damage", "SingleEnemy", 8);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var scorie = Combatant.CreateEnemy("canon.enemy.scorie-rampante", "Scorie", "Skirmisher", 30, [laitier, eclat, contact]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [scorie]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "burn", "Brûlure", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));

        var decision = new ScorieRampanteBossBehavior().DecideAction(new BossDecisionContext(combat, scorie));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.laitier-ardent", "canon.skill.eclat-vitrifie", "canon.skill.contact");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void ScorieRampante_ShouldReform_WhenLowHealth_AndNoDottedTarget()
    {
        var reformation = CreateSkill("canon.skill.reformation", "Buff", "Self", 15, "Magic");
        var contact = CreateSkill("canon.skill.contact", "Damage", "SingleEnemy", 8);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var scorie = Combatant.CreateEnemy("canon.enemy.scorie-rampante", "Scorie", "Skirmisher", 30, [reformation, contact]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [scorie]);
        scorie.ApplyDamage(20); // 10/30 HP ~= 33%, under 40%

        var decision = new ScorieRampanteBossBehavior().DecideAction(new BossDecisionContext(combat, scorie));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.reformation");
        decision.TargetIds.Should().BeEquivalentTo(new[] { scorie.Id.Value });
    }
}
