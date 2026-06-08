using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Defines the content generation rules for a given RoomType:
/// which node types appear and at what relative frequency, and the risk range for non-boss nodes.
/// </summary>
public sealed record RoomTypeGenerationProfile
{
    public RoomTypeGenerationProfile(
        RoomType roomType,
        IReadOnlyList<NodeTypeWeight> nodeTypeWeights,
        int riskMin,
        int riskMax)
    {
        ArgumentNullException.ThrowIfNull(nodeTypeWeights);

        if (nodeTypeWeights.Count == 0)
        {
            throw new DomainException("RoomTypeGenerationProfile must have at least one node type weight.");
        }

        if (riskMin < 0 || riskMin > 100)
        {
            throw new DomainException("RiskMin must be between 0 and 100.");
        }

        if (riskMax < riskMin || riskMax > 100)
        {
            throw new DomainException("RiskMax must be between RiskMin and 100.");
        }

        RoomType = roomType;
        NodeTypeWeights = nodeTypeWeights;
        RiskMin = riskMin;
        RiskMax = riskMax;
        TotalWeight = nodeTypeWeights.Sum(w => w.Weight);
    }

    public RoomType RoomType { get; }

    public IReadOnlyList<NodeTypeWeight> NodeTypeWeights { get; }

    /// <summary>Minimum risk level (inclusive) for non-boss nodes.</summary>
    public int RiskMin { get; }

    /// <summary>Maximum risk level (exclusive) for non-boss nodes.</summary>
    public int RiskMax { get; }

    /// <summary>Sum of all weights; precomputed for efficient weighted selection.</summary>
    public int TotalWeight { get; }
}
