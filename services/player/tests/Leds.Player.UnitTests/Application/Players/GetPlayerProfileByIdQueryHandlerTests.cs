using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class GetPlayerProfileByIdQueryHandlerTests
{
    private readonly Mock<IPlayerProfileRepository> _repository;
    private readonly GetPlayerProfileByIdQueryHandler _handler;

    public GetPlayerProfileByIdQueryHandlerTests()
    {
        _repository = new Mock<IPlayerProfileRepository>();
        _handler = new GetPlayerProfileByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfile_WhenPlayerExists()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(profile.Id.Value);
        result.DisplayName.Should().Be("Test Player");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPlayerDoesNotExist()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var query = new GetPlayerProfileByIdQuery(playerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapCharactersCorrectly()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Characters.Should().HaveCount(1);
        result.Characters.Single().DefinitionKey.Should().Be("character.player.self");
        result.Characters.Single().DisplayName.Should().Be("L'Aventurier");
    }

    [Fact]
    public async Task Handle_ShouldMapProgressionCorrectly()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Progression.TotalRunsStarted.Should().Be(0);
        result.Progression.TotalRunsCompleted.Should().Be(0);
        result.Progression.TotalRunsFailed.Should().Be(0);
        result.Progression.TotalRunsAbandoned.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapTimestampsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test Player", now);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CreatedAtUtc.Should().Be(now);
        result.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task Handle_ShouldMapRehydratedProfileCorrectly()
    {
        var id = PlayerId.New();
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "custom.key", "Custom Character", 150, 10, 5, ["skill.custom"]);
        var roster = PlayerRoster.Rehydrate([character]);
        var progression = PlayerProgression.Rehydrate(10, 5, 3, 2);
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Rehydrated Player", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id.Value);
        result.DisplayName.Should().Be("Rehydrated Player");
        result.Characters.Should().HaveCount(1);
        result.Characters.Single().DefinitionKey.Should().Be("custom.key");
        result.Progression.TotalRunsStarted.Should().Be(10);
        result.Progression.TotalRunsCompleted.Should().Be(5);
        result.Progression.TotalRunsFailed.Should().Be(3);
        result.Progression.TotalRunsAbandoned.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterSkillKeys()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Characters.Single().SkillKeys.Should().Contain("skill.basic.guard");
        result.Characters.Single().SkillKeys.Should().NotContain("skill.basic.strike");
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterStats()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Characters.Single().MaxVitality.Should().Be(100);
        result.Characters.Single().BaseMana.Should().Be(85);
        result.Characters.Single().BaseCharge.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectPlayerId()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var query = new GetPlayerProfileByIdQuery(playerId);
        await _handler.Handle(query, CancellationToken.None);

        _repository.Verify(r => r.GetByIdAsync(
            It.Is<PlayerId>(id => id.Value == playerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCharacters_WhenRosterIsEmpty()
    {
        var id = PlayerId.New();
        var roster = PlayerRoster.Rehydrate([]);
        var progression = PlayerProgression.CreateDefault();
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Empty Roster Player", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerProfileByIdQuery(id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Characters.Should().BeEmpty();
    }
}
