using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Players;

public sealed class PlayerProfileTests
{
    [Fact]
    public void Create_ShouldCreateDefaultCharacter()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);

        profile.Roster.Characters.Should().HaveCount(1);
        profile.Roster.Characters.Single().DefinitionKey.Should().Be("character.player.self");
        profile.Roster.Characters.Single().DisplayName.Should().Be("Le Porteur");
    }

    [Fact]
    public void Create_ShouldRejectEmptyDisplayName()
    {
        var act = () => PlayerProfile.Create("", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*display name*");
    }

    [Fact]
    public void Create_ShouldInitializeProgressionAtZero()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        profile.Progression.TotalRunsStarted.Should().Be(0);
        profile.Progression.TotalRunsCompleted.Should().Be(0);
        profile.Progression.TotalRunsFailed.Should().Be(0);
    }
}

public sealed class PlayerCharacterTests
{
    [Fact]
    public void Create_ShouldRejectInvalidVitality()
    {
        var act = () => PlayerCharacter.Create("key", "Name", 0, 0, 0, ["skill"]);

        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Fact]
    public void Create_ShouldRejectEmptySkillKeys()
    {
        var act = () => PlayerCharacter.Create("key", "Name", 100, 0, 0, []);

        act.Should().Throw<DomainException>().WithMessage("*at least one skill*");
    }
}

public sealed class PlayerRosterTests
{
    [Fact]
    public void AddCharacter_ShouldRejectDuplicateCharacter()
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
        var character1 = PlayerCharacter.Create("key", "Name1", 100, 0, 0, ["skill"]);
        var character2 = PlayerCharacter.Create("key", "Name2", 100, 0, 0, ["skill"]);

        roster.AddCharacter(character1);
        var act = () => roster.AddCharacter(character2);

        act.Should().Throw<DomainException>().WithMessage("*definition key*");
    }
}

public sealed class PlayerProgressionTests
{
    [Fact]
    public void CreateDefault_ShouldStartAtZero()
    {
        var progression = PlayerProgression.CreateDefault();

        progression.TotalRunsStarted.Should().Be(0);
        progression.TotalRunsCompleted.Should().Be(0);
        progression.TotalRunsFailed.Should().Be(0);
    }
}

public sealed class PlayerProfileRehydrateTests
{
    [Fact]
    public void Rehydrate_ShouldRestoreState()
    {
        var id = PlayerId.New();
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "key", "Name", 100, 0, 0, ["skill"]);
        var roster = PlayerRoster.Rehydrate([character]);
        var progression = PlayerProgression.Rehydrate(5, 3, 2);
        var now = DateTimeOffset.UtcNow;

        var profile = PlayerProfile.Rehydrate(id, "Test", roster, progression, now, now);

        profile.Id.Should().Be(id);
        profile.DisplayName.Should().Be("Test");
        profile.Roster.Characters.Should().HaveCount(1);
        profile.Progression.TotalRunsStarted.Should().Be(5);
    }
}
