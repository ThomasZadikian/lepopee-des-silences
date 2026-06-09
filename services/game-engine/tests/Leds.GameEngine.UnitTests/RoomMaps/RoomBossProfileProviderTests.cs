using FluentAssertions;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

namespace Leds.GameEngine.UnitTests.RoomMaps;

public sealed class RoomBossProfileProviderTests
{
    private static IRoomBossProfileResolver CreateSut() =>
        new RoomBossProfileResolver(new InMemoryCatalogContentGateway());

    // -----------------------------------------------------------------------
    // Profile existence + correctness per RoomType
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ShouldReturnThresholdBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Threshold);

        profile.BossId.Should().Be("boss.threshold.warden");
        profile.Name.Should().Be("Gardien du Seuil");
        profile.RoomType.Should().Be(RoomType.Threshold);
        profile.DangerHint.Should().Be("High");
        profile.EnemyTemplateKey.Should().Be("boss.threshold.warden-v1");
    }

    [Fact]
    public async Task ShouldReturnForestBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Forest);

        profile.BossId.Should().Be("boss.forest.rootbound-memory");
        profile.Name.Should().Be("Gardien des Racines");
        profile.RoomType.Should().Be(RoomType.Forest);
        profile.DangerHint.Should().Be("Medium");
        profile.EnemyTemplateKey.Should().Be("boss.forest.rootbound-memory-v1");
    }

    [Fact]
    public async Task ShouldReturnRuptureBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Rupture);

        profile.BossId.Should().Be("boss.rupture.fractured-echo");
        profile.Name.Should().Be("Fragment de Rupture");
        profile.RoomType.Should().Be(RoomType.Rupture);
        profile.DangerHint.Should().Be("High");
        profile.EnemyTemplateKey.Should().Be("boss.rupture.fractured-echo-v1");
    }

    [Fact]
    public async Task ShouldReturnSilenceBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Silence);

        profile.BossId.Should().Be("boss.silence.mute-herald");
        profile.Name.Should().Be("Voix Éteinte");
        profile.RoomType.Should().Be(RoomType.Silence);
        profile.DangerHint.Should().Be("Medium");
        profile.EnemyTemplateKey.Should().Be("boss.silence.mute-herald-v1");
    }

    [Fact]
    public async Task ShouldReturnMemoryBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Memory);

        profile.BossId.Should().Be("boss.memory.archivist");
        profile.Name.Should().Be("Archiviste des Échos");
        profile.RoomType.Should().Be(RoomType.Memory);
        profile.DangerHint.Should().Be("Medium");
        profile.EnemyTemplateKey.Should().Be("boss.memory.archivist-v1");
    }

    // -----------------------------------------------------------------------
    // Distinctness
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ShouldReturnDistinctBossProfiles_ForDifferentRoomTypes()
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

        var profiles = await Task.WhenAll(roomTypes.Select(rt => sut.ResolveAsync(rt)));

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
    public async Task ShouldReturnStableBossProfile_ForSameRoomType(RoomType roomType)
    {
        var sut = CreateSut();

        var profile1 = await sut.ResolveAsync(roomType);
        var profile2 = await sut.ResolveAsync(roomType);

        profile2.BossId.Should().Be(profile1.BossId,
            "the same RoomType must always produce the same BossId.");
        profile2.EnemyTemplateKey.Should().Be(profile1.EnemyTemplateKey,
            "the same RoomType must always produce the same EnemyTemplateKey.");
    }

    // -----------------------------------------------------------------------
    // Additional room types via Catalog
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ShouldReturnAntechamberBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Antechamber);

        profile.BossId.Should().Be("boss.antechamber.last-door");
        profile.Name.Should().Be("Gardien de l'Antichambre");
        profile.RoomType.Should().Be(RoomType.Antechamber);
        profile.DangerHint.Should().Be("Very High");
        profile.EnemyTemplateKey.Should().Be("boss.antechamber.last-door-v1");
    }

    [Fact]
    public async Task ShouldReturnFinalBossProfile()
    {
        var profile = await CreateSut().ResolveAsync(RoomType.Final);

        profile.BossId.Should().Be("boss.final.himlit");
        profile.Name.Should().Be("Him'Lit");
        profile.RoomType.Should().Be(RoomType.Final);
        profile.DangerHint.Should().Be("Extreme");
        profile.EnemyTemplateKey.Should().Be("boss.final.himlit-v1");
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
    [InlineData(RoomType.Antechamber)]
    [InlineData(RoomType.Final)]
    public async Task ShouldHaveAllFieldsPopulated_ForAllRoomTypes(RoomType roomType)
    {
        var profile = await CreateSut().ResolveAsync(roomType);

        profile.BossId.Should().NotBeNullOrWhiteSpace();
        profile.Name.Should().NotBeNullOrWhiteSpace();
        profile.DangerHint.Should().NotBeNullOrWhiteSpace();
        profile.EnemyTemplateKey.Should().NotBeNullOrWhiteSpace();
        profile.RoomType.Should().Be(roomType);
    }
}
