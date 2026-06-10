using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Players;
using Leds.Player.Application.Players.CreatePlayerProfile;
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
}
