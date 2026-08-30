using FluentAssertions;
using Leds.Player.Domain.Players;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Characters;

public sealed class CharacterLifecycleRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Roster_ShouldAllowSeveralPlayableCharactersBasedOnTheSameBaseCharacter()
    {
        var roster = PlayerRoster.Create();
        var first = CreateLegacyCompatibleCharacter("Aster");
        var second = CreateLegacyCompatibleCharacter("Nocturne");

        roster.AddCharacter(first);
        var act = () => roster.AddCharacter(second);

        act.Should().NotThrow(
            "an Account may own several Characters even when they share the same base character definition");
        roster.Characters.Should().HaveCount(2);
    }

    [Fact]
    public void PlayableCharacter_ShouldPersistTheSelectedArchetype()
    {
        var character = CreateFuturePlayableCharacter("Aster", "archetype.porteur");

        FutureContract.Read<string>(character, "ArchetypeKey")
            .Should().Be("archetype.porteur");
    }

    [Fact]
    public void Archetype_ShouldBeImmutableAfterCharacterCreation()
    {
        var character = CreateFuturePlayableCharacter("Aster", "archetype.porteur");

        FutureContract.HasPublicSetter(character, "ArchetypeKey").Should().BeFalse();
    }

    [Fact]
    public void Archive_ShouldHideCharacterWithoutDeletingItFromTheRoster()
    {
        var roster = PlayerRoster.Create();
        var active = CreateLegacyCompatibleCharacter("Aster");
        var archived = CreateLegacyCompatibleCharacter("Nocturne");
        roster.AddCharacter(active);
        roster.AddCharacter(archived);

        FutureContract.InvokeInstance(archived, "Archive", Now);

        FutureContract.Read<bool>(archived, "IsArchived").Should().BeTrue();
        roster.Characters.Should().Contain(archived,
            "archiving is a soft-delete and the row must remain represented in the domain model");
        roster.GetAvailableCharacters().Should().Contain(active).And.NotContain(archived);
    }

    [Fact]
    public void Archive_ShouldBeIdempotent()
    {
        var character = CreateLegacyCompatibleCharacter("Nocturne");

        FutureContract.InvokeInstance(character, "Archive", Now);
        FutureContract.InvokeInstance(character, "Archive", Now.AddHours(1));

        FutureContract.Read<DateTimeOffset?>(character, "ArchivedAtUtc")
            .Should().Be(Now,
                "repeating an archive command must not rewrite the original archival timestamp");
    }

    private static PlayerCharacter CreateLegacyCompatibleCharacter(string displayName)
    {
        return PlayerCharacter.Create(
            definitionKey: "character.player.self",
            displayName: displayName,
            statBlock: PlayerCharacterStatBlock.CreateDefaultPorteur(),
            skills:
            [
                PlayerCharacterSkill.Create(
                    "skill.basic.guard",
                    Now,
                    "tdd",
                    isEquipped: true)
            ]);
    }

    private static object CreateFuturePlayableCharacter(string displayName, string archetypeKey)
    {
        var characterType = typeof(PlayerCharacter);
        return FutureContract.InvokeStatic(
            characterType,
            "CreatePlayable",
            "character.player.self",
            displayName,
            archetypeKey,
            PlayerCharacterStatBlock.CreateDefaultPorteur(),
            new[]
            {
                PlayerCharacterSkill.Create(
                    "skill.basic.guard",
                    Now,
                    "tdd",
                    isEquipped: true)
            });
    }
}
