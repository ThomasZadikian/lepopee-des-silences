using FluentAssertions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class RunPartySelectionPolicyTests
{
    [Fact]
    public void Resolve_ShouldUseSelectedPlayableCharacterAndKeepOnlyCompanions()
    {
        var firstPlayer = Guid.NewGuid();
        var selectedPlayer = Guid.NewGuid();
        var companion = Guid.NewGuid();
        var snapshots = new[]
        {
            Snapshot(firstPlayer, "First"),
            Snapshot(companion, "Companion"),
            Snapshot(selectedPlayer, "Selected")
        };
        var profileCharacters = new[]
        {
            ProfileCharacter(firstPlayer, "Player"),
            ProfileCharacter(selectedPlayer, "Player"),
            ProfileCharacter(companion, "Companion")
        };

        var result = RunPartySelectionPolicy.Resolve(
            snapshots, profileCharacters, selectedPlayer);

        result.Select(character => character.CharacterId)
            .Should().Equal(selectedPlayer, companion);
    }

    [Fact]
    public void Resolve_ShouldRejectUnknownOrNonPlayableSelection()
    {
        var companion = Guid.NewGuid();
        var snapshots = new[] { Snapshot(companion, "Companion") };
        var profileCharacters = new[] { ProfileCharacter(companion, "Companion") };

        var nonPlayable = () => RunPartySelectionPolicy.Resolve(
            snapshots, profileCharacters, companion);
        var unknown = () => RunPartySelectionPolicy.Resolve(
            snapshots, profileCharacters, Guid.NewGuid());

        nonPlayable.Should().Throw<DomainException>().WithMessage("*selected character*");
        unknown.Should().Throw<DomainException>().WithMessage("*selected character*");
    }

    [Fact]
    public void Resolve_ShouldRejectPlayableCharacterMissingFromRunSnapshot()
    {
        var playerId = Guid.NewGuid();

        var action = () => RunPartySelectionPolicy.Resolve(
            [], [ProfileCharacter(playerId, "Player")], playerId);

        action.Should().Throw<DomainException>().WithMessage("*selected character*");
    }

    [Fact]
    public void Resolve_ShouldPreserveLegacySnapshotOrderWhenNoSelectionIsProvided()
    {
        var snapshots = new[]
        {
            Snapshot(Guid.NewGuid(), "First"),
            Snapshot(Guid.NewGuid(), "Second")
        };

        RunPartySelectionPolicy.Resolve(snapshots, [], null).Should().Equal(snapshots);
    }

    private static PlayerRunSnapshotCharacter Snapshot(Guid id, string displayName) =>
        new(
            id,
            "character.player.self",
            displayName,
            new PlayerRunSnapshotCharacterStats(100, 10, 5, 0, 10, 10, 0, 20, 0),
            [new PlayerRunSnapshotCharacterSkill(
                "skill.basic.strike", "Strike", "Damage", "SingleEnemy", "Damage", 0, 0, 10)]);

    private static PlayerCharacterView ProfileCharacter(Guid id, string characterType) =>
        new(
            id,
            characterType == "Player" ? "character.player.self" : "character.mane",
            characterType,
            [],
            new PlayerCharacterStatsView(100, 10, 5, 0, 10, 10, 0, 20, 0),
            4,
            CharacterType: characterType);
}
