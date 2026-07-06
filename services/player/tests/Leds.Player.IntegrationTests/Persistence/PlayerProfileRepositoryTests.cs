using FluentAssertions;
using Leds.Player.Domain.Players;
using Leds.Player.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.IntegrationTests.Persistence;

[Collection("PlayerPostgres")]
public sealed class PlayerProfileRepositoryTests
{
    private readonly PlayerPostgresFixture _fixture;

    public PlayerProfileRepositoryTests(PlayerPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistPermanentUnlock_AndRehydrateIt()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var repository = new EfPlayerProfileRepository(context);

        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", null, DateTimeOffset.UtcNow);
        await repository.SaveAsync(profile, CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(profile.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.PermanentUnlocks.Should().ContainSingle(
            u => u.UnlockKey == "npc.hitomi:offer.skill" && u.UnlockType == "npc-offering");
        reloaded.HasPermanentUnlock("npc.hitomi:offer.skill").Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ShouldNotDuplicateOrThrow_WhenSavedTwiceWithSameUnlock()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using var _ = context;

        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", null, DateTimeOffset.UtcNow);

        var repository = new EfPlayerProfileRepository(context);
        await repository.SaveAsync(profile, CancellationToken.None);

        // Reload through a fresh context (separate change tracker) to simulate a second,
        // independent request granting the same offering — GrantPermanentUnlock is a
        // domain no-op here, but the repository's append-only reconciliation must also
        // never duplicate the row if it were ever called with an untracked duplicate.
        await using var secondContext = _fixture.CreateContext(connectionString);
        var secondRepository = new EfPlayerProfileRepository(secondContext);
        var reloaded = await secondRepository.GetByIdAsync(profile.Id, CancellationToken.None);
        reloaded!.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", null, DateTimeOffset.UtcNow);
        await secondRepository.SaveAsync(reloaded, CancellationToken.None);

        await using var verifyContext = _fixture.CreateContext(connectionString);
        var unlockCount = await verifyContext.PlayerPermanentUnlocks
            .CountAsync(u => u.PlayerProfileId == profile.Id.Value && u.UnlockKey == "npc.hitomi:offer.skill");

        unlockCount.Should().Be(1);
    }
}
