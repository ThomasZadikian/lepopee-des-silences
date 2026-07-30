using FluentAssertions;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.UnitTests.Combats;

/// <summary>
/// Mina's legendary "Protection de Him'Lit" (owned — not equipped): an innate, permanent
/// buff/debuff bundle applied to the protagonist at combat start — see
/// CombatFactory.ApplyHimLitProtection.
/// </summary>
public sealed class CombatFactoryHimLitProtectionTests
{
    private static CombatEncounterDraft CreateDraftWithProtagonist()
    {
        var protagonist = new CombatEncounterDraftAlly(
            "player.self", "Le Porteur", "Fighter", Array.Empty<string>(), IsProtagonist: true);

        var enemy = new CombatEncounterDraftEnemy(
            "enemy.0", "Enemy0", "Description", "Guard", 3, 1, 5,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CombatEncounterDraftSkill>());

        return new CombatEncounterDraft(
            RunId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            RoomType: "Threshold",
            RoomIndex: 1,
            RiskLevel: 3,
            EncounterType: "Combat",
            Enemies: new[] { enemy },
            Allies: new[] { protagonist });
    }

    [Fact]
    public void CreateFromDraft_ShouldNotAlterProtagonist_WhenHimLitProtectionDisabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, attackPower: 12, speed: 10, himLitProtectionEnabled: false);

        var protagonist = combat.Allies.Single();
        protagonist.Guard.Should().Be(0);
        protagonist.StatusEffects.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromDraft_ShouldGrantTenFlatGuard_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, himLitProtectionEnabled: true);

        combat.Allies.Single().Guard.Should().Be(10);
    }

    [Fact]
    public void CreateFromDraft_ShouldBoostAttackPowerAndSpeed_ByFivePercent_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, attackPower: 20, speed: 10, himLitProtectionEnabled: true);

        var protagonist = combat.Allies.Single();
        protagonist.EffectiveAttackPower.Should().Be(21); // 20 + 5%
        protagonist.EffectiveSpeed.Should().Be(9); // +5% and -10% on the base stat
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyTheAdditionalSpeedPenalty_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, himLitProtectionEnabled: true);

        combat.Allies.Single().StatusEffects.Should().Contain(effect =>
            effect.Key == "himlit-protection:tempo"
            && effect.Stat == CombatStat.Speed
            && effect.Magnitude == -10
            && effect.IsMagnitudePercentOfBaseStat);
    }

    [Fact]
    public void CreateFromDraft_ShouldReduceSkillCostByFivePercent_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, himLitProtectionEnabled: true);

        combat.Allies.Single().EffectiveSkillCostReductionPercent.Should().Be(-5);
    }

    [Fact]
    public void CreateFromDraft_ShouldApplyPermanentStatusEffects_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var draft = CreateDraftWithProtagonist();

        var combat = factory.CreateFromDraft(draft, himLitProtectionEnabled: true);

        var protagonist = combat.Allies.Single();
        protagonist.StatusEffects.Should().HaveCount(4);
        protagonist.StatusEffects.Should().OnlyContain(effect =>
            effect.Kind == StatusEffectKind.StatModifier && effect.IsPermanent);
    }

    [Fact]
    public void CreateFromDraft_ShouldNotAffectCompanions_WhenHimLitProtectionEnabled()
    {
        var factory = new CombatFactory();
        var protagonist = new CombatEncounterDraftAlly(
            "player.self", "Le Porteur", "Fighter", Array.Empty<string>(), IsProtagonist: true);
        var companion = new CombatEncounterDraftAlly(
            "companion.mane", "Mané", "Support", Array.Empty<string>(), IsProtagonist: false);
        var enemy = new CombatEncounterDraftEnemy(
            "enemy.0", "Enemy0", "Description", "Guard", 3, 1, 5,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CombatEncounterDraftSkill>());

        var draft = new CombatEncounterDraft(
            RunId: Guid.NewGuid(), RoomId: Guid.NewGuid(), NodeId: Guid.NewGuid(),
            RoomType: "Threshold", RoomIndex: 1, RiskLevel: 3, EncounterType: "Combat",
            Enemies: new[] { enemy }, Allies: new[] { protagonist, companion });

        var combat = factory.CreateFromDraft(draft, himLitProtectionEnabled: true);

        var companionCombatant = combat.Allies.Single(a => a.SourceKey == "companion.mane");
        companionCombatant.Guard.Should().Be(0);
        companionCombatant.StatusEffects.Should().BeEmpty();
    }
}
