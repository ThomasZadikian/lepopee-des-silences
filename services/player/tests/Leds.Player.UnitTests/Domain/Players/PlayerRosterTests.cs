using System.Collections.ObjectModel;
using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class PlayerRosterTests
{
    [Fact]
    public void Create_ShouldReturnEmptyRoster()
    {
        var roster = PlayerRoster.Create();

        roster.Characters.Should().BeEmpty();
    }

    [Fact]
    public void Characters_ShouldReturnReadOnlyCollection()
    {
        var roster = PlayerRoster.Create();

        roster.Characters.Should().BeOfType<ReadOnlyCollection<PlayerCharacter>>();
    }

    [Fact]
    public void AddCharacter_ShouldAddCharacterToRoster()
    {
        var roster = PlayerRoster.Create();
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);

        roster.AddCharacter(character);

        roster.Characters.Should().ContainSingle();
        roster.Characters.Should().Contain(character);
    }

    [Fact]
    public void AddCharacter_ShouldRejectNullCharacter()
    {
        var roster = PlayerRoster.Create();

        var act = () => roster.AddCharacter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCharacter_ShouldRejectDuplicateCharacterId()
    {
        var roster = PlayerRoster.Create();
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);

        roster.AddCharacter(character);
        var act = () => roster.AddCharacter(character);

        act.Should().Throw<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public void AddCharacter_ShouldRejectDuplicateDefinitionKey()
    {
        var roster = PlayerRoster.Create();
        var character1 = PlayerCharacter.Create("key", "Name1", 100, 0, 0, ["skill1"]);
        var character2 = PlayerCharacter.Create("key", "Name2", 100, 0, 0, ["skill2"]);

        roster.AddCharacter(character1);
        var act = () => roster.AddCharacter(character2);

        act.Should().Throw<DomainException>().WithMessage("*definition key*");
    }

    [Fact]
    public void AddCharacter_ShouldAllowMultipleCharactersWithDifferentKeys()
    {
        var roster = PlayerRoster.Create();
        var character1 = PlayerCharacter.Create("key1", "Name1", 100, 0, 0, ["skill1"]);
        var character2 = PlayerCharacter.Create("key2", "Name2", 100, 0, 0, ["skill2"]);

        roster.AddCharacter(character1);
        roster.AddCharacter(character2);

        roster.Characters.Should().HaveCount(2);
    }

    [Fact]
    public void GetAvailableCharacters_ShouldReturnAllCharacters()
    {
        var roster = PlayerRoster.Create();
        var character1 = PlayerCharacter.Create("key1", "Name1", 100, 0, 0, ["skill1"]);
        var character2 = PlayerCharacter.Create("key2", "Name2", 100, 0, 0, ["skill2"]);

        roster.AddCharacter(character1);
        roster.AddCharacter(character2);

        var available = roster.GetAvailableCharacters();

        available.Should().HaveCount(2);
    }

    [Fact]
    public void GetAvailableCharacters_ShouldReturnReadOnlyCollection()
    {
        var roster = PlayerRoster.Create();

        var available = roster.GetAvailableCharacters();

        available.Should().BeOfType<ReadOnlyCollection<PlayerCharacter>>();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreCharacters()
    {
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);

        var roster = PlayerRoster.Rehydrate([character]);

        roster.Characters.Should().ContainSingle();
        roster.Characters.Should().Contain(character);
    }

    [Fact]
    public void Rehydrate_ShouldAllowEmptyCollection()
    {
        var roster = PlayerRoster.Rehydrate([]);

        roster.Characters.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldPreserveCharacterState()
    {
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "key", "Name", 100, 0, 0, ["skill"]);

        var roster = PlayerRoster.Rehydrate([character]);

        roster.Characters.Single().DefinitionKey.Should().Be("key");
        roster.Characters.Single().DisplayName.Should().Be("Name");
        roster.Characters.Single().MaxVitality.Should().Be(100);
    }

    [Fact]
    public void FindById_ShouldReturnMatchingCharacter()
    {
        var roster = PlayerRoster.Create();
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);
        roster.AddCharacter(character);

        var found = roster.FindById(character.Id);

        found.Should().Be(character);
    }

    [Fact]
    public void FindById_ShouldReturnNull_WhenCharacterDoesNotExist()
    {
        var roster = PlayerRoster.Create();

        var found = roster.FindById(PlayerCharacterId.New());

        found.Should().BeNull();
    }

    [Fact]
    public void GetRequired_ShouldReturnMatchingCharacter()
    {
        var roster = PlayerRoster.Create();
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);
        roster.AddCharacter(character);

        var found = roster.GetRequired(character.Id);

        found.Should().Be(character);
    }

    [Fact]
    public void GetRequired_ShouldThrow_WhenCharacterDoesNotExist()
    {
        var roster = PlayerRoster.Create();

        var act = () => roster.GetRequired(PlayerCharacterId.New());

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }
}
