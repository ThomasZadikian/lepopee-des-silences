using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

/// <summary>
/// Direct <see cref="IBossBehavior.DecideAction"/> calls — see the equivalent note
/// in VeilleursDuSeuilBehaviorsTests. Chance-gated branches are asserted as
/// invariants rather than an exact outcome.
/// </summary>
public sealed class GardiensDeCrystalBehaviorsTests
{
    private static CombatantSkill CreateSkill(
        string key, string effectType, string targetingType, int basePower, string category = "Physical")
    {
        return CombatantSkill.Create(
            key, key, effectType, targetingType, effectType, manaCost: 0, chargeCost: 0,
            basePower: basePower, category: category);
    }

    [Fact]
    public void GardienIntemporel_ShouldOpenWithRempart_WhenAnEclatIsPresent_OnFirstTurn()
    {
        var rempart = CreateSkill("canon.skill.rempart", "Buff", "AllAllies", 7);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var gardien = Combatant.CreateEnemy("canon.enemy.gardien-intemporel", "Gardien", "Bruiser", 130, [rempart]);
        var eclat = Combatant.CreateEnemy("canon.enemy.eclat-eveille", "Éclat", "Skirmisher", 44, []);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [gardien, eclat]);

        var decision = new GardienIntemporelBossBehavior().DecideAction(new BossDecisionContext(combat, gardien));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.rempart");
        decision.TargetIds.Should().BeEquivalentTo(new[] { gardien.Id.Value, eclat.Id.Value });
    }

    [Fact]
    public void GardienIntemporel_ShouldNotOpenWithRempart_WhenAloneOrPastFirstTurn()
    {
        var refraction = CreateSkill("canon.skill.refraction", "Damage", "SingleEnemy", 15, "Magic");
        var poing = CreateSkill("canon.skill.poing-de-crystal", "Damage", "SingleEnemy", 17);
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var gardien = Combatant.CreateEnemy("canon.enemy.gardien-intemporel", "Gardien", "Bruiser", 130, [refraction, poing]);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [gardien]);

        var decision = new GardienIntemporelBossBehavior().DecideAction(new BossDecisionContext(combat, gardien));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().BeOneOf("canon.skill.refraction", "canon.skill.poing-de-crystal");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EclatEveille_ShouldOpenWithFacette_OnFirstTurn()
    {
        var facette = CreateSkill("canon.skill.facette", "Buff", "Self", 0, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var eclat = Combatant.CreateEnemy("canon.enemy.eclat-eveille", "Éclat", "Skirmisher", 44, [facette], mana: 30);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [eclat]);

        var decision = new EclatEveilleBossBehavior().DecideAction(new BossDecisionContext(combat, eclat));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.facette");
        decision.TargetIds.Should().BeEquivalentTo(new[] { eclat.Id.Value });
    }

    [Fact]
    public void EclatEveille_ShouldPulse_OnSecondTurn()
    {
        var pulsation = CreateSkill("canon.skill.pulsation", "Damage", "SingleEnemy", 12, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var eclat = Combatant.CreateEnemy("canon.enemy.eclat-eveille", "Éclat", "Skirmisher", 44, [pulsation], mana: 30);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [eclat]);
        combat.AdvanceTurn();

        var decision = new EclatEveilleBossBehavior().DecideAction(new BossDecisionContext(combat, eclat));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.pulsation");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EclatEveille_ShouldUnleashFlammeSeraphine_WhenManaIsHeldInReserve()
    {
        var flamme = CreateSkill("canon.skill.flamme-seraphine", "Damage", "SingleEnemy", 34, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var eclat = Combatant.CreateEnemy("canon.enemy.eclat-eveille", "Éclat", "Skirmisher", 44, [flamme], mana: 30);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [eclat]);
        combat.AdvanceTurn();
        combat.AdvanceTurn(); // turn 3

        var decision = new EclatEveilleBossBehavior().DecideAction(new BossDecisionContext(combat, eclat));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.flamme-seraphine");
        decision.TargetIds.Should().BeEquivalentTo(new[] { hero.Id.Value });
    }

    [Fact]
    public void EclatEveille_ShouldHoldItsReserve_WhenManaIsLow()
    {
        var prisme = CreateSkill("canon.skill.prisme", "Buff", "Self", 12, "Magic");
        var hero = Combatant.CreateAlly("player.1", "Hero", "Fighter", 100);
        var eclat = Combatant.CreateEnemy("canon.enemy.eclat-eveille", "Éclat", "Skirmisher", 44, [prisme], mana: 30);
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), [hero], [eclat]);
        combat.AdvanceTurn();
        combat.AdvanceTurn(); // turn 3
        eclat.SpendMana(20); // 10 mana left, under the 12 reserve

        var decision = new EclatEveilleBossBehavior().DecideAction(new BossDecisionContext(combat, eclat));

        decision.Should().NotBeNull();
        decision!.SkillKey.Should().Be("canon.skill.prisme");
        decision.TargetIds.Should().BeEquivalentTo(new[] { eclat.Id.Value });
    }
}
