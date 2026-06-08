using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps;

/// <summary>
/// Returns statically-defined generation profiles for each supported RoomType.
/// Profiles are deterministic: the same RoomType always yields the same weights and risk range.
/// Antechamber and Final fall back to the Threshold profile.
/// </summary>
public sealed class HardcodedRoomTypeGenerationProfileProvider : IRoomTypeGenerationProfileProvider
{
    private static readonly IReadOnlyDictionary<RoomType, RoomTypeGenerationProfile> Profiles =
        new Dictionary<RoomType, RoomTypeGenerationProfile>
        {
            // ----------------------------------------------------------------
            // Threshold — introductory room; balanced with a Combat bias.
            // Low-to-moderate risk. Single reward option per node type
            // to keep the experience predictable for first-time players.
            // ----------------------------------------------------------------
            [RoomType.Threshold] = new RoomTypeGenerationProfile(
                RoomType.Threshold,
                nodeTypeWeights:
                [
                    new(NodeEventType.Combat,   30),
                    new(NodeEventType.Rest,     15),
                    new(NodeEventType.Item,     15),
                    new(NodeEventType.Npc,      10),
                    new(NodeEventType.Merchant, 10),
                    new(NodeEventType.Law,       5),
                    new(NodeEventType.Curse,     5),
                    new(NodeEventType.Rare,      5),
                    new(NodeEventType.Elite,     5),
                ],
                riskMin: 5,
                riskMax: 61,
                rewardProfilesByNodeType: new Dictionary<NodeEventType, IReadOnlyList<string>>
                {
                    [NodeEventType.Combat]   = ["combat-common"],
                    [NodeEventType.Elite]    = ["elite-low"],
                    [NodeEventType.Rest]     = ["rest-safe"],
                    [NodeEventType.Item]     = ["item-common"],
                    [NodeEventType.Npc]      = ["npc-basic"],
                    [NodeEventType.Merchant] = ["merchant"],
                    [NodeEventType.Law]      = ["law-minor"],
                    [NodeEventType.Curse]    = ["curse-minor"],
                    [NodeEventType.Rare]     = ["rare-low"],
                }),

            // ----------------------------------------------------------------
            // Forest — exploration/support; favours Npc, Rest, Item.
            // Low-to-moderate risk. Rest and Item carry variant reward profiles
            // to reflect the organic richness of the Forest.
            // NOTE: Memory/Narrative are NOT direct MapNode types; Npc, Law,
            // Item and Rest carry the narrative weight instead.
            // ----------------------------------------------------------------
            [RoomType.Forest] = new RoomTypeGenerationProfile(
                RoomType.Forest,
                nodeTypeWeights:
                [
                    new(NodeEventType.Npc,      25),
                    new(NodeEventType.Rest,     20),
                    new(NodeEventType.Item,     20),
                    new(NodeEventType.Combat,   15),
                    new(NodeEventType.Merchant, 10),
                    new(NodeEventType.Rare,      5),
                    new(NodeEventType.Law,       3),
                    new(NodeEventType.Curse,     1),
                    new(NodeEventType.Elite,     1),
                ],
                riskMin: 5,
                riskMax: 51,
                rewardProfilesByNodeType: new Dictionary<NodeEventType, IReadOnlyList<string>>
                {
                    [NodeEventType.Combat]   = ["combat-common"],
                    [NodeEventType.Elite]    = ["elite-low"],
                    [NodeEventType.Rest]     = ["rest-safe", "rest-enhanced"],
                    [NodeEventType.Item]     = ["item-common", "item-uncommon"],
                    [NodeEventType.Npc]      = ["narrative", "npc-basic"],
                    [NodeEventType.Merchant] = ["merchant"],
                    [NodeEventType.Law]      = ["law-minor"],
                    [NodeEventType.Curse]    = ["curse-minor"],
                    [NodeEventType.Rare]     = ["rare-low"],
                }),

            // ----------------------------------------------------------------
            // Rupture — high-risk zone; favours Combat, Elite, Rare, Curse.
            // Medium-to-high risk floor. Reward profiles reflect higher stakes:
            // better loot, but riskier nodes with potential negative effects.
            // ----------------------------------------------------------------
            [RoomType.Rupture] = new RoomTypeGenerationProfile(
                RoomType.Rupture,
                nodeTypeWeights:
                [
                    new(NodeEventType.Combat,   30),
                    new(NodeEventType.Elite,    20),
                    new(NodeEventType.Rare,     15),
                    new(NodeEventType.Curse,    15),
                    new(NodeEventType.Item,      8),
                    new(NodeEventType.Npc,       5),
                    new(NodeEventType.Rest,      5),
                    new(NodeEventType.Merchant,  1),
                    new(NodeEventType.Law,       1),
                ],
                riskMin: 25,
                riskMax: 86,
                rewardProfilesByNodeType: new Dictionary<NodeEventType, IReadOnlyList<string>>
                {
                    [NodeEventType.Combat]   = ["combat-common", "combat-uncommon"],
                    [NodeEventType.Elite]    = ["elite", "elite-risk"],
                    [NodeEventType.Rest]     = ["rest-minimal"],
                    [NodeEventType.Item]     = ["item-uncommon", "item-rare"],
                    [NodeEventType.Npc]      = ["narrative"],
                    [NodeEventType.Merchant] = ["merchant"],
                    [NodeEventType.Law]      = ["law-risk"],
                    [NodeEventType.Curse]    = ["curse-risk"],
                    [NodeEventType.Rare]     = ["rare", "rare-high"],
                }),

            // ----------------------------------------------------------------
            // Silence — enigmatic/systemic; favours Law, Npc, Merchant.
            // Variable risk — less frontal danger, more indirect/systemic.
            // Law and Merchant have richer reward options to reflect player choice.
            // ----------------------------------------------------------------
            [RoomType.Silence] = new RoomTypeGenerationProfile(
                RoomType.Silence,
                nodeTypeWeights:
                [
                    new(NodeEventType.Law,      25),
                    new(NodeEventType.Npc,      25),
                    new(NodeEventType.Merchant, 20),
                    new(NodeEventType.Rest,     10),
                    new(NodeEventType.Item,     10),
                    new(NodeEventType.Combat,    5),
                    new(NodeEventType.Rare,      3),
                    new(NodeEventType.Elite,     1),
                    new(NodeEventType.Curse,     1),
                ],
                riskMin: 5,
                riskMax: 56,
                rewardProfilesByNodeType: new Dictionary<NodeEventType, IReadOnlyList<string>>
                {
                    [NodeEventType.Combat]   = ["combat-common"],
                    [NodeEventType.Elite]    = ["elite-low"],
                    [NodeEventType.Rest]     = ["rest-safe"],
                    [NodeEventType.Item]     = ["item-common"],
                    [NodeEventType.Npc]      = ["narrative"],
                    [NodeEventType.Merchant] = ["merchant", "merchant-special"],
                    [NodeEventType.Law]      = ["law-major", "law-choice"],
                    [NodeEventType.Curse]    = ["curse-modifier"],
                    [NodeEventType.Rare]     = ["rare-low"],
                }),

            // ----------------------------------------------------------------
            // Memory — Tome/fragment-oriented; favours Npc, Law, Item, Rest.
            // Low-to-moderate risk. Item and Npc carry memory-specific profiles
            // to reflect the cognitive weight of the Memory room.
            // NodeEventType.Memory and Narrative are NOT used as direct MapNode
            // types — they are approximated through Npc, Law, Item, Rest.
            // ----------------------------------------------------------------
            [RoomType.Memory] = new RoomTypeGenerationProfile(
                RoomType.Memory,
                nodeTypeWeights:
                [
                    new(NodeEventType.Npc,      30),
                    new(NodeEventType.Law,      25),
                    new(NodeEventType.Item,     20),
                    new(NodeEventType.Rest,     15),
                    new(NodeEventType.Combat,    5),
                    new(NodeEventType.Rare,      3),
                    new(NodeEventType.Merchant,  1),
                    new(NodeEventType.Elite,     1),
                    // NodeEventType.Curse intentionally excluded from Memory rooms
                ],
                riskMin: 5,
                riskMax: 46,
                rewardProfilesByNodeType: new Dictionary<NodeEventType, IReadOnlyList<string>>
                {
                    [NodeEventType.Combat]   = ["combat-common"],
                    [NodeEventType.Elite]    = ["elite-low"],
                    [NodeEventType.Rest]     = ["rest-safe"],
                    [NodeEventType.Item]     = ["item-common", "memory-fragment"],
                    [NodeEventType.Npc]      = ["narrative", "npc-tome"],
                    [NodeEventType.Merchant] = ["merchant"],
                    [NodeEventType.Law]      = ["law-minor"],
                    [NodeEventType.Rare]     = ["rare-low"],
                }),
        };

    public RoomTypeGenerationProfile GetProfile(RoomType roomType)
    {
        if (Profiles.TryGetValue(roomType, out var profile))
        {
            return profile;
        }

        // Antechamber, Final, and any future types fall back to Threshold.
        return Profiles[RoomType.Threshold];
    }
}
