using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.UnitTests.Players;

/// <summary>
/// Effective-stat computation (raw + equipment bonuses), extracted from
/// StartRunCommandHandler so it can be reused by the mid-run stat resync.
/// </summary>
public sealed class PlayerStatMergerTests
{
    private static PlayerRunSnapshotCharacterStats CreateStats() => new(
        MaxVitality: 100, AttackPower: 12, Defense: 6, StartingGuard: 0,
        Speed: 10, Initiative: 10,Focus: 2, Mana: 20, Charge: 3,
        MagicAttack: 4, MagicDefense: 3, Movement: 4);

    [Fact]
    public void ComputeEffectiveStats_ShouldReturnRawStats_WhenNoEquipmentEffects()
    {
        var merger = new PlayerStatMerger();

        var result = merger.ComputeEffectiveStats(CreateStats(), []);

        result.MaxVitality.Should().Be(100);
        result.AttackPower.Should().Be(12);
        result.Defense.Should().Be(6);
        result.Speed.Should().Be(10);
        result.Focus.Should().Be(2);
        result.MagicAttack.Should().Be(4);
        result.MagicDefense.Should().Be(3);
        result.Mana.Should().Be(20);
        result.Charge.Should().Be(3);
        result.Movement.Should().Be(4);
        result.GuardBonusPercent.Should().Be(0);
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldApplyFlatStatBonus()
    {
        var merger = new PlayerStatMerger();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect("StatBonus", StatKind: "AttackPower", Amount: 5, SkillKey: null, AffinityRegister: null),
        };

        var result = merger.ComputeEffectiveStats(CreateStats(), effects);

        result.AttackPower.Should().Be(17);
        result.Defense.Should().Be(6);
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldApplyPercentStatBonus_AgainstTheRawBaseValue()
    {
        var merger = new PlayerStatMerger();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect("StatBonusPercent", StatKind: "Speed", Amount: 10, SkillKey: null, AffinityRegister: null),
        };

        var result = merger.ComputeEffectiveStats(CreateStats(), effects);

        result.Speed.Should().Be(11); // 10 + round(10 * 10 / 100)
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldFoldGuardStatBonusPercent_IntoGuardBonusPercent()
    {
        var merger = new PlayerStatMerger();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect("StatBonusPercent", StatKind: "Guard", Amount: 20, SkillKey: null, AffinityRegister: null),
        };

        var result = merger.ComputeEffectiveStats(CreateStats(), effects);

        result.GuardBonusPercent.Should().Be(20);
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldApplyAdditiveMovementBonuses_WithAMinimumOfOne()
    {
        var merger = new PlayerStatMerger();
        var bonus = new[]
        {
            new CatalogItemEquipmentEffect("StatBonus", StatKind: "Movement", Amount: 2, SkillKey: null, AffinityRegister: null),
        };
        var excessivePenalty = new[]
        {
            new CatalogItemEquipmentEffect("StatBonus", StatKind: "Movement", Amount: -20, SkillKey: null, AffinityRegister: null),
        };

        merger.ComputeEffectiveStats(CreateStats(), bonus).Movement.Should().Be(6);
        merger.ComputeEffectiveStats(CreateStats(), excessivePenalty).Movement.Should().Be(1);
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldExcludeConditionalStatBonus_FromTheStaticBake()
    {
        var merger = new PlayerStatMerger();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonus", StatKind: "AttackPower", Amount: 5, SkillKey: null, AffinityRegister: null,
                Condition: "room:Montagne"),
        };

        var result = merger.ComputeEffectiveStats(CreateStats(), effects);

        // Conditional effects are re-evaluated fresh every combat by CombatFactory instead
        // of being baked into this static run-start computation.
        result.AttackPower.Should().Be(12);
    }

    [Fact]
    public void ComputeEffectiveStats_ShouldExcludeConditionalStatBonusPercent_FromTheStaticBake()
    {
        var merger = new PlayerStatMerger();
        var effects = new[]
        {
            new CatalogItemEquipmentEffect(
                "StatBonusPercent", StatKind: "Speed", Amount: 10, SkillKey: null, AffinityRegister: null,
                Condition: "weather:PluieViolacee"),
        };

        var result = merger.ComputeEffectiveStats(CreateStats(), effects);

        result.Speed.Should().Be(10);
    }
}
