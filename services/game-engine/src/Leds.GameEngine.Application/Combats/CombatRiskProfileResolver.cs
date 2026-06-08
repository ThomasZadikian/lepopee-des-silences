using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Stateless, deterministic implementation of <see cref="ICombatRiskProfileResolver"/>.
///
/// Formula:
///   riskDelta              = max(0, actualRisk − baseRisk)
///   difficultyMultiplier   = clamp(1.0 + riskDelta / 100.0, 1.0, MaxMultiplier)
///   rewardPowerMultiplier  = difficultyMultiplier
///
/// BaseRisk values:
///   Normal   = 20
///   Rare     = 30
///   Elite    = 35
///   RoomBoss = 50
///   FinalBoss= 70
///
/// RiskBand mapping (based on actualRisk):
///   0–24   → Low
///   25–49  → Moderate
///   50–74  → High
///   75–100 → Critical
/// </summary>
public sealed class CombatRiskProfileResolver : ICombatRiskProfileResolver
{
    private const double MaxMultiplier = 1.75;

    private static readonly IReadOnlyDictionary<CombatTier, int> BaseRisks =
        new Dictionary<CombatTier, int>
        {
            [CombatTier.Normal]    = 20,
            [CombatTier.Rare]      = 30,
            [CombatTier.Elite]     = 35,
            [CombatTier.RoomBoss]  = 50,
            [CombatTier.FinalBoss] = 70,
        };

    public CombatRiskProfile Resolve(NodeEventType eventType, int actualRiskLevel)
    {
        if (!IsCombatNodeType(eventType))
        {
            throw new ArgumentException(
                $"NodeEventType '{eventType}' is not a combat type and cannot be risk-scaled.",
                nameof(eventType));
        }

        var tier = ToCombatTier(eventType);
        return BuildProfile(tier, actualRiskLevel);
    }

    public bool IsCombatNodeType(NodeEventType eventType) =>
        eventType is NodeEventType.Combat
                  or NodeEventType.Rare
                  or NodeEventType.Elite
                  or NodeEventType.RoomBoss
                  or NodeEventType.FinalBoss;

    private static CombatTier ToCombatTier(NodeEventType eventType) => eventType switch
    {
        NodeEventType.Rare      => CombatTier.Rare,
        NodeEventType.Elite     => CombatTier.Elite,
        NodeEventType.RoomBoss  => CombatTier.RoomBoss,
        NodeEventType.FinalBoss => CombatTier.FinalBoss,
        _                       => CombatTier.Normal
    };

    private static CombatRiskProfile BuildProfile(CombatTier tier, int actualRiskLevel)
    {
        var baseRisk   = BaseRisks[tier];
        var riskDelta  = Math.Max(0, actualRiskLevel - baseRisk);
        var multiplier = Math.Min(MaxMultiplier, 1.0 + riskDelta / 100.0);
        var band       = ToRiskBand(actualRiskLevel);

        return new CombatRiskProfile(
            Tier:                  tier,
            BaseRisk:              baseRisk,
            ActualRisk:            actualRiskLevel,
            RiskDelta:             riskDelta,
            DifficultyMultiplier:  multiplier,
            RewardPowerMultiplier: multiplier,
            RiskBand:              band);
    }

    private static RiskBand ToRiskBand(int actualRiskLevel) => actualRiskLevel switch
    {
        < 25 => RiskBand.Low,
        < 50 => RiskBand.Moderate,
        < 75 => RiskBand.High,
        _    => RiskBand.Critical
    };
}
