using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Stateless, deterministic implementation of <see cref="ICombatRiskProfileResolver"/>.
/// Every multiplier is a flat per-tier lookup — no more raw-delta formula, since the
/// node's danger is now a single 5-value tier rather than a continuous 0-100 roll.
/// </summary>
public sealed class CombatRiskProfileResolver : ICombatRiskProfileResolver
{
    // BALANCE KNOB — base combat stat multiplier per risk tier. Applied to every enemy in
    // the encounter; Elite/Rare "preferred" picks get an additional bonus on top of this
    // (see CombatEncounterDraftGenerator / EliteStatMultiplier / RareStatMultiplier).
    private static readonly IReadOnlyDictionary<RiskTier, double> DifficultyMultiplierByTier =
        new Dictionary<RiskTier, double>
        {
            [RiskTier.Calme] = 1.00,
            [RiskTier.Tendu] = 1.15,
            [RiskTier.Dangereux] = 1.35,
            [RiskTier.Perilleux] = 1.60,
            [RiskTier.Fatal] = 2.00,
        };

    // BALANCE KNOB — multiplies each loot entry's drop percent before rolling (see
    // EnemyLootRewardBuilder), capped at 100% there.
    private static readonly IReadOnlyDictionary<RiskTier, double> LootMultiplierByTier =
        new Dictionary<RiskTier, double>
        {
            [RiskTier.Calme] = 1.00,
            [RiskTier.Tendu] = 1.10,
            [RiskTier.Dangereux] = 1.25,
            [RiskTier.Perilleux] = 1.45,
            [RiskTier.Fatal] = 1.75,
        };

    // BALANCE KNOB — multiplies reputation gain from resolving this combat.
    private static readonly IReadOnlyDictionary<RiskTier, double> ReputationMultiplierByTier =
        new Dictionary<RiskTier, double>
        {
            [RiskTier.Calme] = 1.00,
            [RiskTier.Tendu] = 1.10,
            [RiskTier.Dangereux] = 1.20,
            [RiskTier.Perilleux] = 1.35,
            [RiskTier.Fatal] = 1.50,
        };

    // BALANCE KNOB — flat Éclats du Palais granted on combat resolution.
    private static readonly IReadOnlyDictionary<RiskTier, int> EclatsBaseAmountByTier =
        new Dictionary<RiskTier, int>
        {
            [RiskTier.Calme] = 0,
            [RiskTier.Tendu] = 1,
            [RiskTier.Dangereux] = 2,
            [RiskTier.Perilleux] = 3,
            [RiskTier.Fatal] = 5,
        };

    public CombatRiskProfile Resolve(NodeEventType eventType, int riskLevel)
    {
        if (!IsCombatNodeType(eventType))
        {
            throw new ArgumentException(
                $"NodeEventType '{eventType}' is not a combat type and cannot be risk-scaled.",
                nameof(eventType));
        }

        if (riskLevel < 1 || riskLevel > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(riskLevel), riskLevel, "Risk level must be between 1 and 5.");
        }

        var tier = (RiskTier)riskLevel;
        var combatTier = ToCombatTier(eventType);

        return new CombatRiskProfile(
            Tier: combatTier,
            RiskTier: tier,
            DifficultyMultiplier: DifficultyMultiplierByTier[tier],
            LootMultiplier: LootMultiplierByTier[tier],
            ReputationMultiplier: ReputationMultiplierByTier[tier],
            EclatsBaseAmount: EclatsBaseAmountByTier[tier]);
    }

    public bool IsCombatNodeType(NodeEventType eventType) =>
        eventType is NodeEventType.Combat
                  or NodeEventType.Rare
                  or NodeEventType.Elite
                  or NodeEventType.RoomBoss
                  or NodeEventType.FinalBoss;

    private static CombatTier ToCombatTier(NodeEventType eventType) => eventType switch
    {
        NodeEventType.Rare => CombatTier.Rare,
        NodeEventType.Elite => CombatTier.Elite,
        NodeEventType.RoomBoss => CombatTier.RoomBoss,
        NodeEventType.FinalBoss => CombatTier.FinalBoss,
        _ => CombatTier.Normal
    };
}
