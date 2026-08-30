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
/// in VeilleursDuSeuilBehaviorsTests. The Promeneur Figé's AI is entirely
/// chance-gated (no deterministic branch), so its test asserts the invariant
/// (always a valid, correctly-targeted decision from its own kit) rather than an
/// exact skill.
/// </summary>
public sealed class FauxHabitantsDuJardinBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category, emotionalRegister: "Neutral");
    }

    [Fact]
    public void PromeneurFige_ShouldAlwaysReturnAValidDecisionFromItsOwnKit()
    {
        var salut = CreateSkill("canon.skill.salut-de-chapeau", "Damage", "SingleEnemy", 10);
        var conversation = CreateSkill("canon.skill.conversation-tranquille", "Debuff", "SingleEnemy", 0, "Magic");
        var pasdepromenade = CreateSkill("canon.skill.pas-de-promenade", "Buff", "Self", 6);
        var sifflotement = CreateSkill("canon.skill.sifflotement", "Damage", "AllEnemies", 8, "Magic");
        var hero1 = Combatant.CreateAlly("player.1", "Hero1", "Fighter", 100);
        var hero2 = Combatant.CreateAlly("player.2", "Hero2", "Fighter", 100);
        var promeneur = Combatant.CreateEnemy(
            "canon.enemy.promeneur-fige", "Promeneur", "Skirmisher", 38, [salut, conversation, pasdepromenade, sifflotement]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero1, hero2], [promeneur]);

        var decision = new PromeneurFigeBossBehavior().DecideAction(new BossDecisionContext(combat, promeneur));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf(
            "canon.skill.salut-de-chapeau",
            "canon.skill.conversation-tranquille",
            "canon.skill.pas-de-promenade",
            "canon.skill.sifflotement");

        if (decision.SkillKey == "canon.skill.pas-de-promenade")
            decision.TargetIds.Should().BeEquivalentTo(new[] { promeneur.Id.Value });
        else if (decision.SkillKey == "canon.skill.sifflotement")
            decision.TargetIds.Should().BeEquivalentTo(new[] { hero1.Id.Value, hero2.Id.Value });
        else
            decision.TargetIds.Should().ContainSingle()
                .And.Contain(id => id == hero1.Id.Value || id == hero2.Id.Value);
    }

    [Fact]
    public void JardinierSansOmbre_ShouldPurgeTheMultiBuffedTarget_AboveAllElse()
    {
        var emondage = CreateSkill("canon.skill.emondage", "Damage", "SingleEnemy", 11);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var jardinier = Combatant.CreateEnemy("canon.enemy.jardinier-sans-ombre", "Jardinier", "Disruptor", 74, [emondage]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [jardinier]);

        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "buff.attack", "Buff Attaque", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: 10, stat: CombatStat.AttackPower));
        hero.ApplyStatusEffect(CombatStatusEffect.Create(
            "buff.speed", "Buff Vitesse", StatusEffectKind.StatModifier,
            currentTick: 0, durationTicks: 5000, magnitude: 10, stat: CombatStat.Speed));

        var decision = new JardinierSansOmbreBossBehavior().DecideAction(new BossDecisionContext(combat, jardinier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.emondage");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void JardinierSansOmbre_ShouldMendAWoundedAlly_WhenNoOneIsMultiBuffed()
    {
        var greffe = CreateSkill("canon.skill.greffe", "Buff", "SingleAlly", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var jardinier = Combatant.CreateEnemy("canon.enemy.jardinier-sans-ombre", "Jardinier", "Disruptor", 74, [greffe]);
        var promeneur = Combatant.CreateEnemy("canon.enemy.promeneur-fige", "Promeneur", "Skirmisher", 38, []);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [hero], [jardinier, promeneur]);
        promeneur.ApplyDamage(25); // 13/38 HP ~= 34%, under 50%

        var decision = new JardinierSansOmbreBossBehavior().DecideAction(new BossDecisionContext(combat, jardinier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.greffe");
        decision.TargetIds.Should().BeEquivalentTo(new[] { promeneur.Id.Value });
    }

    [Fact]
    public void JardinierSansOmbre_ShouldPruneTheWeakestTarget_ByDefault()
    {
        var secateur = CreateSkill("canon.skill.secateur", "Damage", "SingleEnemy", 13);
        var weak = Combatant.CreateAlly("player.1", "Weak", "Fighter", 100);
        var strong = Combatant.CreateAlly("player.2", "Strong", "Fighter", 100);
        var jardinier = Combatant.CreateEnemy("canon.enemy.jardinier-sans-ombre", "Jardinier", "Disruptor", 74, [secateur]);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [weak, strong], [jardinier]);
        weak.ApplyDamage(60);

        var decision = new JardinierSansOmbreBossBehavior().DecideAction(new BossDecisionContext(combat, jardinier));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.secateur");
        decision.TargetIds.Should().BeEquivalentTo(new[] { weak.Id.Value });
    }
}
