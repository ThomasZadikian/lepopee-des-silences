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
/// in VeilleursDuSeuilBehaviorsTests: this sidesteps ATB-scheduler nondeterminism,
/// exercising only the behavior's own deterministic decision logic.
/// </summary>
public sealed class CopistesBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category, emotionalRegister: "Neutral");
    }

    [Fact]
    public void CopisteAveugle_ShouldMarkSoftestTarget_OnFirstTurn()
    {
        var dictee = CreateSkill("canon.skill.dictee", "Debuff", "SingleEnemy", 0, "Magic");
        var soft = Combatant.Create(
            CombatantId.New(), "player.1", "Soft", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 2);
        var tough = Combatant.Create(
            CombatantId.New(), "player.2", "Tough", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            magicDefense: 20);
        var copiste = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Disruptor", 46, [dictee]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [soft, tough], [copiste]);

        var decision = new CopisteAveugleBossBehavior().DecideAction(new BossDecisionContext(combat, copiste));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.dictee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { soft.Id.Value });
    }

    [Fact]
    public void CopisteAveugle_ShouldStrikeMarkedTarget_OnSecondTurn()
    {
        var sursaut = CreateSkill("canon.skill.sursaut-memoriel", "Damage", "SingleEnemy", 12, "Magic");
        var marked = Combatant.CreateAlly("player.1", "Marked", "Fighter", 100);
        var other = Combatant.CreateAlly("player.2", "Other", "Fighter", 100);
        var copiste = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Disruptor", 46, [sursaut]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [marked, other], [copiste]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1); // turn 2

        marked.ApplyStatusEffect(CombatStatusEffect.Create(
            "canon.skill.dictee:StatModifier", "Dictée", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: -4, stat: CombatStat.MagicDefense));

        var decision = new CopisteAveugleBossBehavior().DecideAction(new BossDecisionContext(combat, copiste));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.sursaut-memoriel");
        decision.TargetIds.Should().BeEquivalentTo(new[] { marked.Id.Value });
    }

    [Fact]
    public void CopisteAveugle_ShouldFallBackToPlumeSeche_WhenManaIsLow()
    {
        var lecture = CreateSkill("canon.skill.lecture-des-silences", "Damage", "SingleEnemy", 15, "Magic");
        var plume = CreateSkill("canon.skill.plume-seche", "Damage", "SingleEnemy", 8);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var copiste = Combatant.CreateEnemy(
            "canon.enemy.copiste-aveugle", "Copiste", "Disruptor", 46, [lecture, plume], mana: 20);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [copiste]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 3); // turn 4, past the scripted opening
        copiste.SpendMana(15); // 5 mana left, under the 10 threshold

        var decision = new CopisteAveugleBossBehavior().DecideAction(new BossDecisionContext(combat, copiste));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.plume-seche");
    }

    [Fact]
    public void EncrierVivant_ShouldRechargeStarvedAlly_BeforeAnythingElse()
    {
        var recharge = CreateSkill("canon.skill.recharge", "Buff", "SingleAlly", 8, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var encrier = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, [recharge]);
        var starvedAlly = Combatant.CreateEnemy("canon.enemy.copiste-aveugle", "Copiste", "Disruptor", 46, [], mana: 20);
        // TacticalCombat.Create immediately begins the first active combatant's turn,
        // which regenerates 5% of MaxMana (here +1) — whoever that turns out to be per
        // initiative order. Spend enough that the fixture stays under the 12 threshold
        // even if starvedAlly itself gets that regen tick (20-10=10, +1 worst case = 11).
        starvedAlly.SpendMana(10);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [encrier, starvedAlly]);

        var decision = new EncrierVivantBossBehavior().DecideAction(new BossDecisionContext(combat, encrier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.recharge");
        decision.TargetIds.Should().BeEquivalentTo(new[] { starvedAlly.Id.Value });
    }

    [Fact]
    public void EncrierVivant_ShouldMarkUndottedTarget_WhenNoAllyIsStarved()
    {
        var encre = CreateSkill("canon.skill.encre-vive", "Debuff", "SingleEnemy", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var encrier = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, [encre]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [encrier]);

        var decision = new EncrierVivantBossBehavior().DecideAction(new BossDecisionContext(combat, encrier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.encre-vive");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EncrierVivant_ShouldProtectItself_WhenBelowHalfHealth_AndFewOpponents()
    {
        var encre = CreateSkill("canon.skill.encre-vive", "Debuff", "SingleEnemy", 0, "Magic");
        var corps = CreateSkill("canon.skill.corps-de-verre", "Buff", "Self", 10);
        var hero1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var hero2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var encrier = Combatant.CreateEnemy("canon.enemy.encrier-vivant", "Encrier", "Support", 58, [encre, corps]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero1, hero2], [encrier]);

        // Every living player already carries a DoT, so the "mark undotted" branch
        // is skipped, and there are only 2 opponents (< 3), so the chance-gated
        // Éclaboussure branch is skipped too — this isolates the deterministic
        // HP-gated self-protection branch.
        foreach (var hero in new[] { hero1, hero2 })
        {
            hero.ApplyStatusEffect(CombatStatusEffect.Create(
                "poison", "Poison", StatusEffectKind.DamageOverTime,
                currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        }
        encrier.ApplyDamage(30); // 28/58 HP ~= 48%, under 50%

        var decision = new EncrierVivantBossBehavior().DecideAction(new BossDecisionContext(combat, encrier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.corps-de-verre");
        decision.TargetIds.Should().BeEquivalentTo(new[] { encrier.Id.Value });
    }

    [Fact]
    public void PageInachevee_ShouldFinishSilencedTarget_Regardless()
    {
        var phrase = CreateSkill("canon.skill.phrase-inachevee", "Damage", "SingleEnemy", 12, "Magic");
        var silence = CreateSkill("canon.skill.silence", "Debuff", "SingleEnemy", 0, "Magic");
        var silenced = Combatant.CreateAlly("player.1", "Silenced", "Fighter", 100);
        var other = Combatant.CreateAlly("player.2", "Other", "Fighter", 100);
        var page = Combatant.CreateEnemy("canon.enemy.page-inachevee", "Page", "Disruptor", 36, [phrase, silence]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [silenced, other], [page]);

        silenced.ApplyStatusEffect(CombatStatusEffect.Create(
            "silence", "Silence", StatusEffectKind.Silence, currentTick: 0, durationTicks: 5000));

        var decision = new PageInacheveeBossBehavior().DecideAction(new BossDecisionContext(combat, page));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.phrase-inachevee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { silenced.Id.Value });
    }

    [Fact]
    public void PageInachevee_ShouldOpenWithSilence_OnFastestPlayer_OnFirstTurn()
    {
        var silence = CreateSkill("canon.skill.silence", "Debuff", "SingleEnemy", 0, "Magic");
        var slow = Combatant.Create(
            CombatantId.New(), "player.1", "Slow", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 5);
        var fast = Combatant.Create(
            CombatantId.New(), "player.2", "Fast", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 20);
        var page = Combatant.CreateEnemy("canon.enemy.page-inachevee", "Page", "Disruptor", 36, [silence]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [slow, fast], [page]);

        var decision = new PageInacheveeBossBehavior().DecideAction(new BossDecisionContext(combat, page));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.silence");
        decision.TargetIds.Should().BeEquivalentTo(new[] { fast.Id.Value });
    }

    [Fact]
    public void Relieur_ShouldExtendDot_OnHeavilyDottedTarget_AboveExecuteThreshold()
    {
        var ecriture = CreateSkill("canon.skill.ecriture-continuelle", "Debuff", "SingleEnemy", 25, "Magic");
        var couture = CreateSkill("canon.skill.couture", "Damage", "SingleEnemy", 16);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var relieur = Combatant.CreateEnemy("canon.enemy.relieur", "Relieur", "Bruiser", 92, [ecriture, couture]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [relieur]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "dot1", "DoT 1", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "dot2", "DoT 2", StatusEffectKind.DamageOverTime,
            currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        // hero stays at full HP (>= 35%), so the execute-chance branch never triggers.

        var decision = new RelieurBossBehavior().DecideAction(new BossDecisionContext(combat, relieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.ecriture-continuelle");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void Relieur_ShouldBindTwoOpponents_OnFirstTurn_WhenNoneIsHeavilyDotted()
    {
        var reliure = CreateSkill("canon.skill.reliure-de-chair", "Debuff", "SingleEnemy", 0, "Magic");
        var weak = Combatant.CreateAlly("player.1", "Weak", "Fighter", 40);
        var strong = Combatant.CreateAlly("player.2", "Strong", "Fighter", 100);
        var relieur = Combatant.CreateEnemy("canon.enemy.relieur", "Relieur", "Bruiser", 92, [reliure]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [weak, strong], [relieur]);

        var decision = new RelieurBossBehavior().DecideAction(new BossDecisionContext(combat, relieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.reliure-de-chair");
        decision.TargetIds.Should().BeEquivalentTo(new[] { weak.Id.Value });
    }

    [Fact]
    public void Relieur_ShouldStrikeSlowestPlayer_WhenNotFirstTurn_AndNoneIsBoundOrDotted()
    {
        var couture = CreateSkill("canon.skill.couture", "Damage", "SingleEnemy", 16);
        var slow = Combatant.Create(
            CombatantId.New(), "player.1", "Slow", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 4);
        var fast = Combatant.Create(
            CombatantId.New(), "player.2", "Fast", CombatantSide.Player, "Fighter",
            maxVitality: 100, currentVitality: 100, guard: 0, baseGuard: 0, mana: 0, charge: 0,
            speed: 18);
        var relieur = Combatant.CreateEnemy("canon.enemy.relieur", "Relieur", "Bruiser", 92, [couture]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [slow, fast], [relieur]);
        TestTacticalCombatHelper.AdvanceRounds(combat, 1);

        var decision = new RelieurBossBehavior().DecideAction(new BossDecisionContext(combat, relieur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.couture");
        decision.TargetIds.Should().BeEquivalentTo(new[] { slow.Id.Value });
    }
}
