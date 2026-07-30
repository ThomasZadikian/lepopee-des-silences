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
        MagicAttack: 4, MagicDefense: 3);

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
}
