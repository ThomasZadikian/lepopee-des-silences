using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class GetPlayerRunSnapshotQueryHandlerTests
{
    private readonly Mock<IPlayerProfileRepository> _repository;
    private readonly GetPlayerRunSnapshotQueryHandler _handler;

    public GetPlayerRunSnapshotQueryHandlerTests()
    {
        _repository = new Mock<IPlayerProfileRepository>();
        _handler = new GetPlayerRunSnapshotQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSnapshot_WhenPlayerExists()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.PlayerId.Should().Be(profile.Id.Value);
        result.DisplayName.Should().Be("Test Player");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var query = new GetPlayerRunSnapshotQuery(playerId);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Player*");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WithPlayerId_WhenPlayerDoesNotExist()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var query = new GetPlayerRunSnapshotQuery(playerId);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{playerId}*");
    }

    [Fact]
    public async Task Handle_ShouldReturnAvailableCharacters()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Characters.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenNoCharactersAvailable()
    {
        var id = PlayerId.New();
        var roster = PlayerRoster.Rehydrate([]);
        var progression = PlayerProgression.CreateDefault();
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Empty Player", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(id.Value);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*No available characters*");
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterDetails()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Characters.Should().HaveCount(1);
        var character = result.Characters.Single();
        character.DefinitionKey.Should().Be("character.player.self");
        character.DisplayName.Should().Be("Le Porteur");
        character.MaxVitality.Should().Be(100);
        character.BaseMana.Should().Be(0);
        character.BaseCharge.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterSkillKeys()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(profile.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        var character = result.Characters.Single();
        character.SkillKeys.Should().Contain("skill.basic.strike");
        character.SkillKeys.Should().Contain("skill.basic.guard");
    }

    [Fact]
    public async Task Handle_ShouldReturnMultipleCharacters_WhenRosterHasMultiple()
    {
        var id = PlayerId.New();
        var character1 = PlayerCharacter.Create("key1", "Character 1", 100, 0, 0, ["skill1"]);
        var character2 = PlayerCharacter.Create("key2", "Character 2", 150, 10, 5, ["skill2"]);
        var roster = PlayerRoster.Create();
        roster.AddCharacter(character1);
        roster.AddCharacter(character2);
        var progression = PlayerProgression.CreateDefault();
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Multi Character", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Characters.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectPlayerId()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var query = new GetPlayerRunSnapshotQuery(playerId);

        try
        {
            await _handler.Handle(query, CancellationToken.None);
        }
        catch (NotFoundException)
        {
        }

        _repository.Verify(r => r.GetByIdAsync(
            It.Is<PlayerId>(id => id.Value == playerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCustomCharacterDetails()
    {
        var id = PlayerId.New();
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "custom.key", "Custom Hero", 200, 50, 10, ["skill.custom.attack", "skill.custom.defend"]);
        var roster = PlayerRoster.Rehydrate([character]);
        var progression = PlayerProgression.CreateDefault();
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Custom Player", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var query = new GetPlayerRunSnapshotQuery(id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Characters.Should().HaveCount(1);
        var charResponse = result.Characters.Single();
        charResponse.DefinitionKey.Should().Be("custom.key");
        charResponse.DisplayName.Should().Be("Custom Hero");
        charResponse.MaxVitality.Should().Be(200);
        charResponse.BaseMana.Should().Be(50);
        charResponse.BaseCharge.Should().Be(10);
        charResponse.SkillKeys.Should().BeEquivalentTo(["skill.custom.attack", "skill.custom.defend", "skill.basic.strike"]);
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnEquippedSkillKeys()
    {
        var id = PlayerId.New();
        var now = DateTimeOffset.UtcNow;
        var skills = new[]
        {
            PlayerCharacterSkill.Create("skill.equipped.a", now, isEquipped: true),
            PlayerCharacterSkill.Create("skill.equipped.b", now, isEquipped: true),
            PlayerCharacterSkill.Create("skill.known.but.unequipped", now, isEquipped: false),
        };
        var character = PlayerCharacter.Create(
            "key", "Name", PlayerCharacterStatBlock.CreateDefaultPorteur(), skills);
        var roster = PlayerRoster.Rehydrate([character]);
        var progression = PlayerProgression.CreateDefault();
        var profile = PlayerProfile.Rehydrate(id, "Test", roster, progression, now, now);

        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(id.Value), CancellationToken.None);

        var charResponse = result.Characters.Single();
        charResponse.SkillKeys.Should().BeEquivalentTo(["skill.equipped.a", "skill.equipped.b", "skill.basic.strike"]);
        charResponse.SkillKeys.Should().NotContain("skill.known.but.unequipped");
    }

    [Fact]
    public async Task Handle_ShouldPopulateFullStatBlock()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        var character = result.Characters.Single();
        character.Stats.Should().NotBeNull();
        character.Stats!.MaxVitality.Should().Be(100);
        character.Stats.AttackPower.Should().Be(12);
        character.Stats.Defense.Should().Be(6);
        character.Stats.Speed.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldReflectSpentStatPointsInStatBlock()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var characterId = profile.Roster.Characters.Single().Id;
        profile.AwardStatPoint(DateTimeOffset.UtcNow);
        profile.SpendStatPoint(characterId, PlayerStatKind.AttackPower, DateTimeOffset.UtcNow);

        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        result.Characters.Single().Stats!.AttackPower.Should().Be(13);
    }
}
