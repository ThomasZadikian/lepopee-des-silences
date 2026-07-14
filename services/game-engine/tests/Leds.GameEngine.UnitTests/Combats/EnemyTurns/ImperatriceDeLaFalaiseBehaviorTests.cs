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
public sealed class ImperatriceDeLaFalaiseBehaviorTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void Imperatrice_ShouldOpenWithDelugeDuStyx_OnFirstTurn()
    {
        var deluge = CreateSkill("canon.skill.deluge-du-styx", "Debuff", "AllEnemies", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var imperatrice = Combatant.CreateEnemy("canon.enemy.imperatrice", "Impératrice", "Bruiser", 240, [deluge]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [imperatrice]);

        var decision = new ImperatriceBossBehavior().DecideAction(new BossDecisionContext(combat, imperatrice));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.deluge-du-styx");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void Imperatrice_ShouldNeverTargetLameDeFond_OnAFullHealthOpponent()
    {
        var lame = CreateSkill("canon.skill.lame-de-fond", "Damage", "SingleEnemy", 26);
        var maree = CreateSkill("canon.skill.maree-montante", "Debuff", "AllEnemies", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var imperatrice = Combatant.CreateEnemy("canon.enemy.imperatrice", "Impératrice", "Bruiser", 240, [lame, maree]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [imperatrice]);
        combat.AdvanceTurn(); // move past the turn-1 opener; hero stays at full HP

        var decision = new ImperatriceBossBehavior().DecideAction(new BossDecisionContext(combat, imperatrice));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.maree-montante");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void Imperatrice_ShouldSummonSymphonie_WhenAtOrBelowSixtyPercent_AndSomeoneIsUndotted()
    {
        var symphonie = CreateSkill("canon.skill.symphonie-des-enfers", "Damage", "AllEnemies", 6, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var imperatrice = Combatant.CreateEnemy("canon.enemy.imperatrice", "Impératrice", "Bruiser", 240, [symphonie]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [imperatrice]);
        combat.AdvanceTurn();
        imperatrice.ApplyDamage(120); // 120/240 = 50%, at or below 60%

        var decision = new ImperatriceBossBehavior().DecideAction(new BossDecisionContext(combat, imperatrice));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.symphonie-des-enfers");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void Imperatrice_ShouldReinforceLameDeFond_WhenBelowTwentyFivePercent_AndTargetIsHeavilyDotted()
    {
        var lameRenforcee = CreateSkill("canon.skill.lame-de-fond-renforcee", "Damage", "SingleEnemy", 39);
        var lame = CreateSkill("canon.skill.lame-de-fond", "Damage", "SingleEnemy", 26);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var imperatrice = Combatant.CreateEnemy("canon.enemy.imperatrice", "Impératrice", "Bruiser", 240, [lameRenforcee, lame]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [imperatrice]);
        combat.AdvanceTurn();
        imperatrice.ApplyDamage(210); // 30/240 = 12.5%, under 25%
        hero.ApplyDamage(20); // hero is now damaged, so Lame de fond can target it

        for (var i = 0; i < 2; i++)
        {
            hero.ApplyStatusEffect(CombatStatusEffect.Create(
                $"poison{i}", "Poison", StatusEffectKind.DamageOverTime,
                currentTick: 0, durationTicks: 5000, magnitude: 5, tickInterval: 1400));
        }

        var decision = new ImperatriceBossBehavior().DecideAction(new BossDecisionContext(combat, imperatrice));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.lame-de-fond-renforcee");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }
}
