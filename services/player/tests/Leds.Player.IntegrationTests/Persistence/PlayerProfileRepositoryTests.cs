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
        reloaded.PermanentUnlocks.Should().ContainSingle(
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

    [Fact]
    public async Task SaveAsync_ShouldPersistPermanentItemAndEquippedCharacterItem_AndRehydrateThem()
    {
        var (context, _) = _fixture.CreateContext();
        await using var _ = context;
        var repository = new EfPlayerProfileRepository(context);

        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var character = profile.CreatePlayableCharacter("Aster", "archetype.porteur", now);
        profile.AddPermanentItems(["item.sac-a-dos"], null, now);
        profile.EquipItem(character.Id, "item.sac-a-dos", now);
        await repository.SaveAsync(profile, CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(profile.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.PermanentItems.Should().ContainSingle(i => i.ItemDefinitionKey == "item.sac-a-dos");
        var reloadedCharacter = reloaded.Roster.Characters.Single();
        reloadedCharacter.EquippedItemKeys.Should().Contain("item.sac-a-dos");
        reloadedCharacter.ArchetypeKey.Should().Be("archetype.porteur");
    }

    [Fact]
    public async Task SaveAsync_ShouldNotDuplicateOrThrow_WhenSavedTwiceWithSamePermanentItem()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using var _ = context;

        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.AddPermanentItems(["item.sac-a-dos"], null, DateTimeOffset.UtcNow);

        var repository = new EfPlayerProfileRepository(context);
        await repository.SaveAsync(profile, CancellationToken.None);

        await using var secondContext = _fixture.CreateContext(connectionString);
        var secondRepository = new EfPlayerProfileRepository(secondContext);
        var reloaded = await secondRepository.GetByIdAsync(profile.Id, CancellationToken.None);
        reloaded!.AddPermanentItems(["item.sac-a-dos"], null, DateTimeOffset.UtcNow);
        await secondRepository.SaveAsync(reloaded, CancellationToken.None);

        await using var verifyContext = _fixture.CreateContext(connectionString);
        var itemCount = await verifyContext.PlayerPermanentItems
            .CountAsync(i => i.PlayerProfileId == profile.Id.Value && i.ItemDefinitionKey == "item.sac-a-dos");

        itemCount.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistContainedLiquid_OnAnAlreadyOwnedPermanentItem()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using var _ = context;

        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        profile.AddPermanentItems(["item.fiole-cristal"], null, now);

        var repository = new EfPlayerProfileRepository(context);
        await repository.SaveAsync(profile, CancellationToken.None);

        await using var secondContext = _fixture.CreateContext(connectionString);
        var secondRepository = new EfPlayerProfileRepository(secondContext);
        var reloaded = await secondRepository.GetByIdAsync(profile.Id, CancellationToken.None);
        reloaded!.SetPermanentItemContent("item.fiole-cristal", "item.larme-de-racine", now);
        await secondRepository.SaveAsync(reloaded, CancellationToken.None);

        await using var verifyContext = _fixture.CreateContext(connectionString);
        var verifyRepository = new EfPlayerProfileRepository(verifyContext);
        var reloadedAgain = await verifyRepository.GetByIdAsync(profile.Id, CancellationToken.None);

        reloadedAgain!.PermanentItems.Should().ContainSingle(i =>
            i.ItemDefinitionKey == "item.fiole-cristal" &&
            i.ContainedLiquidDefinitionKey == "item.larme-de-racine");
    }

    [Fact]
    public async Task SaveAsync_ShouldResumeMainStoryAtThePersistedCheckpoint()
    {
        var (context, connectionString) = _fixture.CreateContext();
        await using var _ = context;
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.AdvanceMainStory(new MainStoryAdvance
        {
            SequenceKey = "story.main",
            SequenceVersion = "1.0",
            StepKey = "step.hall",
            CheckpointKey = "checkpoint.hall",
            UnlockedRoomKeys = ["room.hall"],
            VisibleRoomKeys = ["room.hall", "room.threshold"],
            Complete = false,
            Now = DateTimeOffset.UtcNow
        });
        await new EfPlayerProfileRepository(context).SaveAsync(profile, CancellationToken.None);

        await using var reloadContext = _fixture.CreateContext(connectionString);
        var reloaded = await new EfPlayerProfileRepository(reloadContext)
            .GetByIdAsync(profile.Id, CancellationToken.None);

        reloaded!.MainStoryProgress.StepKey.Should().Be("step.hall");
        reloaded.MainStoryProgress.CheckpointKey.Should().Be("checkpoint.hall");
        reloaded.MainStoryProgress.UnlockedRoomKeys.Should().Contain("room.hall");
        reloaded.MainStoryProgress.VisibleRoomKeys.Should().Contain("room.threshold");
    }
}
