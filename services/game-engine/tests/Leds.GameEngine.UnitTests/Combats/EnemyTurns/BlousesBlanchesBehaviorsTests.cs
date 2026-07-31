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
public sealed class BlousesBlanchesBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void InfirmiereDeni_ShouldOpenWithPlacebo_OnFirstTurn()
    {
        var placebo = CreateSkill("canon.skill.placebo", "Buff", "Self", 10, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var infirmiere = Combatant.CreateEnemy("canon.enemy.infirmiere-deni", "Infirmière", "Disruptor", 68, [placebo]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [infirmiere]);

        var decision = new InfirmiereDeniBossBehavior().DecideAction(new BossDecisionContext(combat, infirmiere));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.placebo");
        decision.TargetIds.Should().BeEquivalentTo(new[] { infirmiere.Id.Value });
    }

    [Fact]
    public void InfirmiereDeni_ShouldFallBackToBordageOrAnagramme_WhenNoOneIsBuffed()
    {
        var bordage = CreateSkill("canon.skill.bordage", "Debuff", "SingleEnemy", 0, "Magic");
        var anagramme = CreateSkill("canon.skill.anagramme", "Damage", "SingleEnemy", 17, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var infirmiere = Combatant.CreateEnemy("canon.enemy.infirmiere-deni", "Infirmière", "Disruptor", 68, [bordage, anagramme]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [infirmiere]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // move past the turn-1 opener

        var decision = new InfirmiereDeniBossBehavior().DecideAction(new BossDecisionContext(combat, infirmiere));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.bordage");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void SouvenirAlite_ShouldMarkUndottedTarget_FirstPriority()
    {
        var nevrose = CreateSkill("canon.skill.nevrose", "Damage", "SingleEnemy", 10, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var souvenir = Combatant.CreateEnemy("canon.enemy.souvenir-alite", "Souvenir", "Skirmisher", 56, [nevrose]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [souvenir]);

        var decision = new SouvenirAliteBossBehavior().DecideAction(new BossDecisionContext(combat, souvenir));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.nevrose");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void SouvenirAlite_ShouldHarassLeastDefendedTarget_WhenEveryoneIsAlreadyDotted()
    {
        var visite = CreateSkill("canon.skill.visite", "Damage", "SingleEnemy", 14, "Magic");
        var drap = CreateSkill("canon.skill.drap-tendu", "Debuff", "SingleEnemy", 0);
        var sturdy = Combatant.Create(
            CombatantId.New(), "player.1", "Sturdy", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 20);
        var frail = Combatant.Create(
            CombatantId.New(), "player.2", "Frail", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            defense: 2);
        var souvenir = Combatant.CreateEnemy("canon.enemy.souvenir-alite", "Souvenir", "Skirmisher", 56, [visite, drap]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [sturdy, frail], [souvenir]);

        foreach (var hero in new[] { sturdy, frail })
        {
            hero.ApplyStatusEffect(CombatStatusEffect.Create(
                "poison", "Poison", StatusEffectKind.DamageOverTime,
                currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        }

        var decision = new SouvenirAliteBossBehavior().DecideAction(new BossDecisionContext(combat, souvenir));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.visite");
        decision.TargetIds.Should().BeEquivalentTo(new[] { frail.Id.Value });
    }

    [Fact]
    public void RegisseurBlanc_ShouldOpenSequence_OnHighestHpPlayer()
    {
        var contemplation = CreateSkill("canon.skill.contemplation-infinie", "Debuff", "SingleEnemy", 0, "Magic");
        var weak = Combatant.CreateAlly("player.1", "Weak", "Fighter", 40);
        var strong = Combatant.CreateAlly("player.2", "Strong", "Fighter", 100);
        var regisseur = Combatant.CreateEnemy("canon.enemy.regisseur-blanc", "Régisseur", "Support", 96, [contemplation]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [weak, strong], [regisseur]);

        var decision = new RegisseurBlancBossBehavior().DecideAction(new BossDecisionContext(combat, regisseur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.contemplation-infinie");
        decision.TargetIds.Should().BeEquivalentTo(new[] { strong.Id.Value });
    }

    [Fact]
    public void RegisseurBlanc_ShouldContinueSequence_OnTheSameSlowedTarget()
    {
        var tourdeclef = CreateSkill("canon.skill.tour-de-clef", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var regisseur = Combatant.CreateEnemy("canon.enemy.regisseur-blanc", "Régisseur", "Support", 96, [tourdeclef]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [regisseur]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.contemplation-infinie:StatModifier", "Contemplation infinie", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -25, stat: CombatStat.Speed));

        var decision = new RegisseurBlancBossBehavior().DecideAction(new BossDecisionContext(combat, regisseur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.tour-de-clef");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void RegisseurBlanc_ShouldFinishTheSequence_OnceBothMarksAreOnTheSameTarget()
    {
        var extinction = CreateSkill("canon.skill.extinction-des-feux", "Damage", "SingleEnemy", 24, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var regisseur = Combatant.CreateEnemy("canon.enemy.regisseur-blanc", "Régisseur", "Support", 96, [extinction]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [regisseur]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.contemplation-infinie:StatModifier", "Contemplation infinie", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -25, stat: CombatStat.Speed));
        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.tour-de-clef:StatModifier", "Tour de clef", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -10, stat: CombatStat.AttackPower));

        var decision = new RegisseurBlancBossBehavior().DecideAction(new BossDecisionContext(combat, regisseur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.extinction-des-feux");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }
}
