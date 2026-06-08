using FluentAssertions;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomTypeGenerationProfileProviderTests
{
    private static IRoomTypeGenerationProfileProvider CreateSut()
        => new HardcodedRoomTypeGenerationProfileProvider();

    // -----------------------------------------------------------------------
    // Profile existence
    // -----------------------------------------------------------------------

    [Fact]
    public void GetProfile_ShouldReturnThresholdProfile()
    {
        var profile = CreateSut().GetProfile(RoomType.Threshold);

        profile.Should().NotBeNull();
        profile.RoomType.Should().Be(RoomType.Threshold);
        profile.NodeTypeWeights.Should().NotBeEmpty();
        profile.TotalWeight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetProfile_ShouldReturnForestProfile()
    {
        var profile = CreateSut().GetProfile(RoomType.Forest);

        profile.Should().NotBeNull();
        profile.RoomType.Should().Be(RoomType.Forest);
        profile.NodeTypeWeights.Should().NotBeEmpty();
    }

    [Fact]
    public void GetProfile_ShouldReturnRuptureProfile()
    {
        var profile = CreateSut().GetProfile(RoomType.Rupture);

        profile.Should().NotBeNull();
        profile.RoomType.Should().Be(RoomType.Rupture);
        profile.NodeTypeWeights.Should().NotBeEmpty();
    }

    [Fact]
    public void GetProfile_ShouldReturnSilenceProfile()
    {
        var profile = CreateSut().GetProfile(RoomType.Silence);

        profile.Should().NotBeNull();
        profile.RoomType.Should().Be(RoomType.Silence);
        profile.NodeTypeWeights.Should().NotBeEmpty();
    }

    [Fact]
    public void GetProfile_ShouldReturnMemoryProfile()
    {
        var profile = CreateSut().GetProfile(RoomType.Memory);

        profile.Should().NotBeNull();
        profile.RoomType.Should().Be(RoomType.Memory);
        profile.NodeTypeWeights.Should().NotBeEmpty();
    }

    // -----------------------------------------------------------------------
    // Determinism
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GetProfile_ShouldReturnDeterministicProfiles(RoomType roomType)
    {
        var sut = CreateSut();

        var profile1 = sut.GetProfile(roomType);
        var profile2 = sut.GetProfile(roomType);

        profile2.TotalWeight.Should().Be(profile1.TotalWeight,
            because: "The same RoomType must always yield the same profile.");

        profile2.RiskMin.Should().Be(profile1.RiskMin);
        profile2.RiskMax.Should().Be(profile1.RiskMax);
    }

    // -----------------------------------------------------------------------
    // Fallback for unsupported types
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Antechamber)]
    [InlineData(RoomType.Final)]
    public void GetProfile_ShouldFallbackToThresholdProfile_ForUnsupportedRoomType(RoomType roomType)
    {
        var sut = CreateSut();

        var fallbackProfile = sut.GetProfile(roomType);
        var thresholdProfile = sut.GetProfile(RoomType.Threshold);

        fallbackProfile.TotalWeight.Should().Be(thresholdProfile.TotalWeight,
            because: "Unsupported RoomTypes fall back to the Threshold profile.");
    }

    // -----------------------------------------------------------------------
    // Risk range consistency
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void GetProfile_ShouldHaveValidRiskRange(RoomType roomType)
    {
        var profile = CreateSut().GetProfile(roomType);

        profile.RiskMin.Should().BeGreaterThanOrEqualTo(0);
        profile.RiskMax.Should().BeGreaterThan(profile.RiskMin);
        profile.RiskMax.Should().BeLessThanOrEqualTo(100);
    }

    // -----------------------------------------------------------------------
    // Rupture: risk range must be higher than Threshold
    // -----------------------------------------------------------------------

    [Fact]
    public void GetProfile_RuptureProfile_ShouldHaveHigherRiskThanThreshold()
    {
        var sut = CreateSut();
        var threshold = sut.GetProfile(RoomType.Threshold);
        var rupture = sut.GetProfile(RoomType.Rupture);

        rupture.RiskMin.Should().BeGreaterThanOrEqualTo(threshold.RiskMin,
            because: "Rupture is a high-risk zone.");
    }

    // -----------------------------------------------------------------------
    // Memory: must not include Memory or Narrative node types directly
    // -----------------------------------------------------------------------

    [Fact]
    public void GetProfile_MemoryProfile_ShouldNotContainDirectMemoryNodeType()
    {
        var profile = CreateSut().GetProfile(RoomType.Memory);

        profile.NodeTypeWeights
            .Should().NotContain(w => w.NodeType == NodeEventType.Memory,
                because: "Memory is not a supported MapNode type and must not appear in the profile.");
    }

    // -----------------------------------------------------------------------
    // A-group — per-profile identity tests (PR 0.1.8)
    // -----------------------------------------------------------------------

    [Fact]
    public void ThresholdProfile_ShouldFavorBalancedIntroNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Threshold);
        var weightOf = (NodeEventType t) => profile.NodeTypeWeights
            .FirstOrDefault(w => w.NodeType == t)?.Weight ?? 0;

        // Combat must be the dominant type (intro bias)
        weightOf(NodeEventType.Combat).Should().BeGreaterThanOrEqualTo(weightOf(NodeEventType.Rest),
            because: "Threshold is a combat-biased introductory room.");
        weightOf(NodeEventType.Combat).Should().BeGreaterThanOrEqualTo(weightOf(NodeEventType.Item));

        // Risky types (Elite, Curse) must each stay below Combat
        weightOf(NodeEventType.Elite).Should().BeLessThan(weightOf(NodeEventType.Combat));
        weightOf(NodeEventType.Curse).Should().BeLessThan(weightOf(NodeEventType.Combat));
    }

    [Fact]
    public void ForestProfile_ShouldFavorSupportExplorationNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Forest);
        var total = profile.TotalWeight;
        var support = profile.NodeTypeWeights
            .Where(w => w.NodeType is NodeEventType.Npc or NodeEventType.Rest or NodeEventType.Item)
            .Sum(w => w.Weight);

        ((double)support / total).Should().BeGreaterThan(0.55,
            because: "Forest favours Npc, Rest, Item — combined weight should exceed 55 %.");
    }

    [Fact]
    public void RuptureProfile_ShouldFavorRiskAndCombatNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Rupture);
        var total = profile.TotalWeight;
        var risky = profile.NodeTypeWeights
            .Where(w => w.NodeType is NodeEventType.Combat or NodeEventType.Elite
                                   or NodeEventType.Rare   or NodeEventType.Curse)
            .Sum(w => w.Weight);

        ((double)risky / total).Should().BeGreaterThan(0.60,
            because: "Rupture favours Combat, Elite, Rare, Curse — combined weight should exceed 60 %.");
    }

    [Fact]
    public void SilenceProfile_ShouldFavorLawNpcAndMerchantNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Silence);
        var total = profile.TotalWeight;
        var systemic = profile.NodeTypeWeights
            .Where(w => w.NodeType is NodeEventType.Law or NodeEventType.Npc or NodeEventType.Merchant)
            .Sum(w => w.Weight);

        ((double)systemic / total).Should().BeGreaterThan(0.55,
            because: "Silence favours Law, Npc, Merchant — combined weight should exceed 55 %.");
    }

    [Fact]
    public void MemoryProfile_ShouldFavorNpcLawItemAndRestNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Memory);
        var total = profile.TotalWeight;
        var memory = profile.NodeTypeWeights
            .Where(w => w.NodeType is NodeEventType.Npc or NodeEventType.Law
                                   or NodeEventType.Item or NodeEventType.Rest)
            .Sum(w => w.Weight);

        ((double)memory / total).Should().BeGreaterThan(0.80,
            because: "Memory favours Npc, Law, Item, Rest — combined weight should exceed 80 %.");
    }

    [Fact]
    public void Profiles_ShouldHaveDistinctNodeTypeWeights()
    {
        var sut = CreateSut();
        var roomTypes = new[] { RoomType.Threshold, RoomType.Forest, RoomType.Rupture, RoomType.Silence, RoomType.Memory };

        // Dominant node type (highest weight) must differ between Threshold and Rupture,
        // and between Forest and Silence — a proxy for meaningful differentiation.
        var dominant = (RoomType rt) => sut.GetProfile(rt).NodeTypeWeights
            .OrderByDescending(w => w.Weight).First().NodeType;

        dominant(RoomType.Rupture).Should().NotBe(dominant(RoomType.Forest),
            because: "Rupture and Forest have clearly different dominant node types.");

        dominant(RoomType.Silence).Should().NotBe(dominant(RoomType.Threshold),
            because: "Silence and Threshold have clearly different dominant node types.");
    }

    [Fact]
    public void Profiles_ShouldHaveDistinctRiskProfiles()
    {
        var sut = CreateSut();

        var thresholdMin = sut.GetProfile(RoomType.Threshold).RiskMin;
        var ruptureMin = sut.GetProfile(RoomType.Rupture).RiskMin;
        var memoryMax = sut.GetProfile(RoomType.Memory).RiskMax;
        var ruptureMax = sut.GetProfile(RoomType.Rupture).RiskMax;

        ruptureMin.Should().BeGreaterThan(thresholdMin,
            because: "Rupture's risk floor must be higher than Threshold's.");
        ruptureMax.Should().BeGreaterThan(memoryMax,
            because: "Rupture's risk ceiling must exceed Memory's.");
    }

    [Fact]
    public void Profiles_ShouldHaveRewardRules()
    {
        var sut = CreateSut();
        var roomTypes = new[] { RoomType.Threshold, RoomType.Forest, RoomType.Rupture, RoomType.Silence, RoomType.Memory };

        foreach (var roomType in roomTypes)
        {
            var profile = sut.GetProfile(roomType);

            profile.RewardProfilesByNodeType.Should().NotBeEmpty(
                because: $"{roomType} profile must define reward rules for at least one node type.");

            foreach (var (nodeType, options) in profile.RewardProfilesByNodeType)
            {
                options.Should().NotBeEmpty(
                    because: $"{roomType}.{nodeType} must have at least one reward profile option.");
                options.Should().AllSatisfy(o => o.Should().NotBeNullOrWhiteSpace());
            }
        }
    }

    [Fact]
    public void RuptureProfile_ShouldHaveRiskierRewardProfiles_ThanThreshold()
    {
        var sut = CreateSut();
        var threshold = sut.GetProfile(RoomType.Threshold);
        var rupture = sut.GetProfile(RoomType.Rupture);

        // Rupture Combat has 2 profiles (including "combat-uncommon"); Threshold only 1
        rupture.RewardProfilesByNodeType[NodeEventType.Combat].Should().Contain("combat-uncommon",
            because: "Rupture combat nodes should offer higher-stakes reward variants.");

        threshold.RewardProfilesByNodeType[NodeEventType.Combat].Should().NotContain("combat-uncommon",
            because: "Threshold keeps combat rewards predictable with a single 'combat-common' profile.");
    }

    [Fact]
    public void MemoryProfile_ShouldHaveMemoryFragmentRewardOption_ForItemNodes()
    {
        var profile = CreateSut().GetProfile(RoomType.Memory);

        profile.RewardProfilesByNodeType[NodeEventType.Item].Should().Contain("memory-fragment",
            because: "Memory Item nodes carry Tome-fragment reward semantics.");
    }

    [Fact]
    public void SilenceProfile_ShouldHaveLawChoiceRewardOption()
    {
        var profile = CreateSut().GetProfile(RoomType.Silence);

        profile.RewardProfilesByNodeType[NodeEventType.Law].Should().Contain("law-choice",
            because: "Silence Law nodes emphasise player-choice reward semantics.");
    }
}
