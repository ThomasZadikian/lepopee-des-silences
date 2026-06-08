using FluentAssertions;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomBossProfileProviderTests
{
    private static IRoomBossProfileResolver CreateSut() => new RoomBossProfileResolver();

    // -----------------------------------------------------------------------
    // Profile existence + correctness per RoomType
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldReturnThresholdBossProfile()
    {
        var profile = CreateSut().Resolve(RoomType.Threshold);

        profile.BossId.Should().Be("threshold-guardian");
        profile.Name.Should().Be("Gardien du Seuil");
        profile.RoomType.Should().Be(RoomType.Threshold);
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().Be("boss-threshold-guardian-v1");
    }

    [Fact]
    public void ShouldReturnForestBossProfile()
    {
        var profile = CreateSut().Resolve(RoomType.Forest);

        profile.BossId.Should().Be("forest-guardian");
        profile.Name.Should().Be("Gardien des Racines");
        profile.RoomType.Should().Be(RoomType.Forest);
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().Be("boss-forest-guardian-v1");
    }

    [Fact]
    public void ShouldReturnRuptureBossProfile()
    {
        var profile = CreateSut().Resolve(RoomType.Rupture);

        profile.BossId.Should().Be("rupture-warden");
        profile.Name.Should().Be("Fragment de Rupture");
        profile.RoomType.Should().Be(RoomType.Rupture);
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().Be("boss-rupture-warden-v1");
    }

    [Fact]
    public void ShouldReturnSilenceBossProfile()
    {
        var profile = CreateSut().Resolve(RoomType.Silence);

        profile.BossId.Should().Be("silence-warden");
        profile.Name.Should().Be("Voix Éteinte");
        profile.RoomType.Should().Be(RoomType.Silence);
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().Be("boss-silence-warden-v1");
    }

    [Fact]
    public void ShouldReturnMemoryBossProfile()
    {
        var profile = CreateSut().Resolve(RoomType.Memory);

        profile.BossId.Should().Be("memory-keeper");
        profile.Name.Should().Be("Archiviste des Échos");
        profile.RoomType.Should().Be(RoomType.Memory);
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().Be("boss-memory-keeper-v1");
    }

    // -----------------------------------------------------------------------
    // Distinctness
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldReturnDistinctBossProfiles_ForDifferentRoomTypes()
    {
        var sut = CreateSut();
        var roomTypes = new[]
        {
            RoomType.Threshold,
            RoomType.Forest,
            RoomType.Rupture,
            RoomType.Silence,
            RoomType.Memory
        };

        var profiles = roomTypes.Select(sut.Resolve).ToArray();

        profiles.Select(p => p.BossId).Should().OnlyHaveUniqueItems(
            "each supported RoomType must have a distinct BossId.");

        profiles.Select(p => p.EnemyTemplateKey).Should().OnlyHaveUniqueItems(
            "each supported RoomType must have a distinct EnemyTemplateKey.");
    }

    // -----------------------------------------------------------------------
    // Stability / determinism
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void ShouldReturnStableBossProfile_ForSameRoomType(RoomType roomType)
    {
        var sut = CreateSut();

        var profile1 = sut.Resolve(roomType);
        var profile2 = sut.Resolve(roomType);

        profile2.BossId.Should().Be(profile1.BossId,
            "the same RoomType must always produce the same BossId.");
        profile2.EnemyTemplateKey.Should().Be(profile1.EnemyTemplateKey,
            "the same RoomType must always produce the same EnemyTemplateKey.");
    }

    // -----------------------------------------------------------------------
    // Fallback for unsupported types
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Antechamber)]
    [InlineData(RoomType.Final)]
    public void ShouldReturnNonEmptyProfile_ForUnsupportedRoomType(RoomType roomType)
    {
        var profile = CreateSut().Resolve(roomType);

        profile.BossId.Should().NotBeNullOrWhiteSpace(
            "unsupported RoomTypes must fall back to a valid profile rather than throw.");
        profile.EnemyTemplateKey.Should().NotBeNullOrWhiteSpace();
        profile.Name.Should().NotBeNullOrWhiteSpace();
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
    }

    // -----------------------------------------------------------------------
    // All fields populated
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(RoomType.Threshold)]
    [InlineData(RoomType.Forest)]
    [InlineData(RoomType.Rupture)]
    [InlineData(RoomType.Silence)]
    [InlineData(RoomType.Memory)]
    public void ShouldHaveAllFieldsPopulated_ForSupportedRoomTypes(RoomType roomType)
    {
        var profile = CreateSut().Resolve(roomType);

        profile.BossId.Should().NotBeNullOrWhiteSpace();
        profile.Name.Should().NotBeNullOrWhiteSpace();
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().NotBeNullOrWhiteSpace();
        profile.RoomType.Should().Be(roomType);
    }
}
