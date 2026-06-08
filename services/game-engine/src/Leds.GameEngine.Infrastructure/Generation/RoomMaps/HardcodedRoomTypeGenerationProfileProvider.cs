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
            // Threshold — introductory room; balanced with a Combat bias
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
                riskMax: 61),

            // ----------------------------------------------------------------
            // Forest — exploration/support; favours Npc, Rest, Item over combat
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
                riskMax: 56),

            // ----------------------------------------------------------------
            // Rupture — high-risk zone; favours Combat, Elite, Rare, Curse
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
                riskMax: 86),

            // ----------------------------------------------------------------
            // Silence — enigmatic; favours Law, Npc, Merchant over combat
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
                riskMax: 51),

            // ----------------------------------------------------------------
            // Memory — narrative/cognitive; favours Npc, Law, Item, Rest.
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
                riskMax: 46),
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
