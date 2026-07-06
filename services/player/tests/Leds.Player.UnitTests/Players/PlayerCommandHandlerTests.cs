using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Players;
using Leds.Player.Application.Players.ClaimNpcOffering;
using Leds.Player.Application.Players.CreatePlayerProfile;
using Leds.Player.Application.Players.GrantNpcReputationMilestone;
using Leds.Player.Application.Players.HasClaimedNpcOffering;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Players;

public sealed class PlayerCommandHandlerTests
{
    [Fact]
    public async Task CreatePlayerProfile_ShouldPersistProfile()
    {
        var repository = new Mock<IPlayerProfileRepository>();
        var handler = new CreatePlayerProfileCommandHandler(repository.Object, TimeProvider.System);

        var response = await handler.Handle(
            new CreatePlayerProfileCommand("Test Player"),
            CancellationToken.None);

        response.Profile.DisplayName.Should().Be("Test Player");
        response.Profile.Characters.Should().HaveCount(1);
        repository.Verify(r => r.SaveAsync(It.IsAny<PlayerProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlayerProfileById_ShouldReturnProfile_WhenExists()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new GetPlayerProfileByIdQueryHandler(repository.Object);
        var result = await handler.Handle(
            new GetPlayerProfileByIdQuery(profile.Id.Value),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Test");
    }

    [Fact]
    public async Task GetPlayerProfileById_ShouldReturnNull_WhenMissing()
    {
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var handler = new GetPlayerProfileByIdQueryHandler(repository.Object);
        var result = await handler.Handle(
            new GetPlayerProfileByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerRunSnapshot_ShouldReturnAvailableCharacters()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new GetPlayerRunSnapshotQueryHandler(repository.Object);
        var result = await handler.Handle(
            new GetPlayerRunSnapshotQuery(profile.Id.Value),
            CancellationToken.None);

        result.Characters.Should().HaveCount(1);
        result.Characters.Single().SkillKeys.Should().Contain("skill.basic.strike");
    }

    [Fact]
    public async Task ClaimNpcOffering_ShouldGrantPermanentUnlockAndPersist()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new ClaimNpcOfferingCommandHandler(repository.Object, TimeProvider.System);
        var sourceRunId = Guid.NewGuid();

        var response = await handler.Handle(
            new ClaimNpcOfferingCommand(profile.Id.Value, "npc.hitomi", "offer.skill", sourceRunId),
            CancellationToken.None);

        response.PermanentUnlocks.Should().ContainSingle(u => u.UnlockKey == "npc.hitomi:offer.skill" && u.UnlockType == "npc-offering");
        repository.Verify(r => r.SaveAsync(It.IsAny<PlayerProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantNpcReputationMilestone_ShouldGrantPermanentUnlockAndPersist()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new GrantNpcReputationMilestoneCommandHandler(repository.Object, TimeProvider.System);

        var response = await handler.Handle(
            new GrantNpcReputationMilestoneCommand(profile.Id.Value, "npc.hitomi", "trust-earned", null),
            CancellationToken.None);

        response.PermanentUnlocks.Should().ContainSingle(
            u => u.UnlockKey == "npc.hitomi:trust-earned" && u.UnlockType == "npc-reputation-milestone");
    }

    [Fact]
    public async Task HasClaimedNpcOffering_ShouldReturnFalse_WhenNeverClaimed()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new HasClaimedNpcOfferingQueryHandler(repository.Object);

        var result = await handler.Handle(
            new HasClaimedNpcOfferingQuery(profile.Id.Value, "npc.hitomi", "offer.skill"),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasClaimedNpcOffering_ShouldReturnTrue_AfterClaiming()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", null, DateTimeOffset.UtcNow);
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new HasClaimedNpcOfferingQueryHandler(repository.Object);

        var result = await handler.Handle(
            new HasClaimedNpcOfferingQuery(profile.Id.Value, "npc.hitomi", "offer.skill"),
            CancellationToken.None);

        result.Should().BeTrue();
    }
}
