using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

/// <summary>
/// Node-type/reward-profile selection rules shared by every room generator, regardless of
/// whether the room's spatial shape is the Classic row/lane DAG (<see cref="MapRoomGenerator"/>)
/// or a Tactical free-movement grid (<see cref="GridRoomGenerator"/>) — <see cref="RoomTypeGenerationProfile"/>
/// only describes node-type weights/risk range/reward pools, which are independent of shape.
/// Extracted from <see cref="MapRoomGenerator"/> (pure refactor, no behavior change).
/// </summary>
public static class NodeGenerationHeuristics
{
    public static NodeEventType PickWeightedNodeType(RoomTypeGenerationProfile profile, Random random)
    {
        var roll = random.Next(profile.TotalWeight);
        var cumulative = 0;

        foreach (var weight in profile.NodeTypeWeights)
        {
            cumulative += weight.Weight;
            if (roll < cumulative)
            {
                return weight.NodeType;
            }
        }

        return profile.NodeTypeWeights[0].NodeType;
    }

    public static string PickRewardProfile(
        NodeEventType type,
        RoomTypeGenerationProfile profile,
        Random random)
    {
        if (profile.RewardProfilesByNodeType.TryGetValue(type, out var options) && options.Count > 0)
        {
            return options.Count == 1 ? options[0] : options[random.Next(options.Count)];
        }

        return type switch
        {
            NodeEventType.Combat => "combat-common",
            NodeEventType.Elite => "elite",
            NodeEventType.Rest => "rest-safe",
            NodeEventType.Item => "item-common",
            NodeEventType.Npc => "narrative",
            NodeEventType.Merchant => "merchant",
            NodeEventType.Law => "law",
            NodeEventType.Curse => "curse",
            NodeEventType.Rare => "rare",
            _ => "standard"
        };
    }

    /// <summary>
    /// Combat danger tier is the sole difficulty axis, derived from the same generation roll —
    /// but only meaningful for combat-flavored nodes ("le risque n'a de sens que pour les combats").
    /// </summary>
    public static RiskTier? DeriveCombatRiskTier(NodeEventType type, int riskLevel)
    {
        return MapNode.IsCombatFlavored(type)
            ? (RiskTier)Math.Clamp(riskLevel / 20 + 1, 1, 5)
            : null;
    }
}
