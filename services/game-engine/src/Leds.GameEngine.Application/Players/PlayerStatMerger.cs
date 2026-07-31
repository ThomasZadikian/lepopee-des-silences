using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Shared "effective stat" computation: combines a character's raw base stats
/// (from player-service's run snapshot) with equipment-derived bonuses (flat
/// <c>StatBonus</c> and percentage <c>StatBonusPercent</c> equipment effects) into
/// the values actually used in combat. Extracted from <c>StartRunCommandHandler</c>
/// so the mid-run stat resync (<c>SyncPartyStatsCommandHandler</c>) can reuse the
/// exact same computation instead of duplicating it and silently drifting.
/// </summary>
public sealed class PlayerStatMerger
{
    public EffectiveCharacterStats ComputeEffectiveStats(
        PlayerRunSnapshotCharacterStats stats,
        IReadOnlyCollection<CatalogItemEquipmentEffect> equipmentEffects)
    {
        var statBonuses = equipmentEffects
            .Where(e => string.Equals(e.Kind, "StatBonus", StringComparison.OrdinalIgnoreCase)
                && e.StatKind is not null
                && e.Amount is not null)
            .GroupBy(e => e.StatKind!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount!.Value), StringComparer.OrdinalIgnoreCase);

        int StatBonus(string statKind) => statBonuses.TryGetValue(statKind, out var value) ? value : 0;

        // Percentage bonuses (e.g. Bague du courage: +10% Speed, +10% AttackPower) —
        // the default way to author stat-boosting equipment going forward. Computed
        // against the character's raw base stat, independent of any flat StatBonus,
        // then both are added to the base (see EffectiveStat below).
        var statBonusPercents = equipmentEffects
            .Where(e => string.Equals(e.Kind, "StatBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.StatKind is not null
                && e.Amount is not null)
            .GroupBy(e => e.StatKind!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount!.Value), StringComparer.OrdinalIgnoreCase);

        int StatBonusPercent(string statKind) => statBonusPercents.TryGetValue(statKind, out var value) ? value : 0;

        int EffectiveStat(string statKind, int baseValue) =>
            baseValue + StatBonus(statKind) + (int)Math.Round(baseValue * StatBonusPercent(statKind) / 100.0);

        return new EffectiveCharacterStats(
            MaxVitality: EffectiveStat("MaxVitality", stats.MaxVitality),
            AttackPower: EffectiveStat("AttackPower", stats.AttackPower),
            Defense: EffectiveStat("Defense", stats.Defense),
            Speed: EffectiveStat("Speed", stats.Speed),
            Focus: EffectiveStat("Focus", stats.Focus),
            MagicAttack: EffectiveStat("MagicAttack", stats.MagicAttack),
            MagicDefense: EffectiveStat("MagicDefense", stats.MagicDefense),
            Mana: EffectiveStat("Mana", stats.Mana),
            // Charge is no longer allocatable, but this merger preserves the current run
            // value. Combat creation/reset remains the owner of the canonical 0..5 gauge.
            Charge: stats.Charge,
            Movement: Math.Max(1, EffectiveStat("Movement", stats.Movement)),
            RunItemCapacity: EffectiveStat("RunItemCapacity", Run.DefaultRunItemCapacity),
            // e.g. Bague de Iris: +20% of whatever Guard the protagonist would otherwise
            // start combat with (Law/climate-derived — see CombatFactory's guardBonus).
            GuardBonusPercent: StatBonusPercent("Guard"));
    }
}

public sealed record EffectiveCharacterStats(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int Speed,
    int Focus,
    int MagicAttack,
    int MagicDefense,
    int Mana,
    int Charge,
    int Movement,
    int RunItemCapacity,
    int GuardBonusPercent);
