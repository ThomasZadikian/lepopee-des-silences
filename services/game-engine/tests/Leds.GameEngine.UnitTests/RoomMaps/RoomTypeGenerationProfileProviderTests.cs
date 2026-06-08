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
}
