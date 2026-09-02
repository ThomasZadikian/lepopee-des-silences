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
        var profile = CreateProfileWithPlayableCharacter();
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

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

        var act = () => _handler.Handle(new GetPlayerRunSnapshotQuery(playerId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Player*");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WithPlayerId_WhenPlayerDoesNotExist()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var act = () => _handler.Handle(new GetPlayerRunSnapshotQuery(playerId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{playerId}*");
    }

    [Fact]
    public async Task Handle_ShouldReturnAvailableCharacters()
    {
        var profile = CreateProfileWithPlayableCharacter();
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        result.Characters.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenNoCharactersAvailable()
    {
        var id = PlayerId.New();
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Empty Player", PlayerRoster.Rehydrate([]), PlayerProgression.CreateDefault(), now, now);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var act = () => _handler.Handle(new GetPlayerRunSnapshotQuery(id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*No available characters*");
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterDetails()
    {
        var profile = CreateProfileWithPlayableCharacter();
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        var character = result.Characters.Should().ContainSingle().Subject;
        character.DefinitionKey.Should().Be("character.player.self");
        character.DisplayName.Should().Be("L'Aventurier");
        character.MaxVitality.Should().Be(100);
        character.BaseMana.Should().Be(85);
        character.BaseCharge.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapCharacterSkillKeys()
    {
        var profile = CreateProfileWithPlayableCharacter();
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        var character = result.Characters.Single();
        character.SkillKeys.Should().Contain("skill.basic.strike");
        character.SkillKeys.Should().Contain("skill.basic.guard");
    }

    [Fact]
    public async Task Handle_ShouldReturnMultipleCharacters_WhenRosterHasMultiple()
    {
        var id = PlayerId.New();
        var roster = PlayerRoster.Create();
        roster.AddCharacter(PlayerCharacter.Create("key1", "Character 1", 100, 0, 0, ["skill1"]));
        roster.AddCharacter(PlayerCharacter.Create("key2", "Character 2", 150, 10, 5, ["skill2"]));
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Multi Character", roster, PlayerProgression.CreateDefault(), now, now);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(id.Value), CancellationToken.None);

        result.Characters.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectPlayerId()
    {
        var playerId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        try { await _handler.Handle(new GetPlayerRunSnapshotQuery(playerId), CancellationToken.None); }
        catch (NotFoundException) { }

        _repository.Verify(r => r.GetByIdAsync(
            It.Is<PlayerId>(id => id.Value == playerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCustomCharacterDetails()
    {
        var id = PlayerId.New();
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "custom.key", "Custom Hero", 200, 50, 10,
            ["skill.custom.attack", "skill.custom.defend"]);
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Rehydrate(id, "Custom Player", PlayerRoster.Rehydrate([character]), PlayerProgression.CreateDefault(), now, now);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(id.Value), CancellationToken.None);

        var charResponse = result.Characters.Should().ContainSingle().Subject;
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
        var character = PlayerCharacter.Create(
            "key", "Name", PlayerCharacterStatBlock.CreateDefaultPorteur(),
            [
                PlayerCharacterSkill.Create("skill.equipped.a", now, isEquipped: true),
                PlayerCharacterSkill.Create("skill.equipped.b", now, isEquipped: true),
                PlayerCharacterSkill.Create("skill.known.but.unequipped", now, isEquipped: false),
            ],
            status: "Active");
        var profile = PlayerProfile.Rehydrate(id, "Test", PlayerRoster.Rehydrate([character]), PlayerProgression.CreateDefault(), now, now);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(id.Value), CancellationToken.None);

        var charResponse = result.Characters.Single();
        charResponse.SkillKeys.Should().BeEquivalentTo(["skill.equipped.a", "skill.equipped.b", "skill.basic.strike"]);
        charResponse.SkillKeys.Should().NotContain("skill.known.but.unequipped");
    }

    [Fact]
    public async Task Handle_ShouldPopulateFullStatBlock()
    {
        var profile = CreateProfileWithPlayableCharacter();
        _repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPlayerRunSnapshotQuery(profile.Id.Value), CancellationToken.None);

        var character = result.Characters.Single();
        character.Stats.Should().NotBeNull();
        character.Stats!.MaxVitality.Should().Be(100);
        character.Stats.AttackPower.Should().Be(12);
        character.Stats.Defense.Should().Be(6);
        character.Stats.Speed.Should().Be(10);
    }

    private static PlayerProfile CreateProfileWithPlayableCharacter()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test Player", now);
        profile.CreatePlayableCharacter("L'Aventurier", "archetype.porteur", now);
        return profile;
    }
}
