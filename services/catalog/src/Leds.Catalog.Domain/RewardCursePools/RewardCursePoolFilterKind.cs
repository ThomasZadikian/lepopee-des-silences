namespace Leds.Catalog.Domain.RewardCursePools;

// Per-entry availability gates: the POOL is contextually filtered (decision Q16a),
// then the draw is equiprobable within the remaining entries (decision Q5b).
public enum RewardCursePoolFilterKind
{
    MinVitalityRatioPercent = 0,
    MaxVitalityRatioPercent = 1,
    MinActiveLawCount = 2,
    MinNodeDepth = 3
}