using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunPlayerSnapshotCoverageTests
{
    [Fact]
    public void Create_ShouldTrimNameAndRetainCharacters()
    {
        var character = Character("character.main");
        var createdAt = DateTimeOffset.UtcNow;

        var snapshot = RunPlayerSnapshot.Create(Guid.NewGuid(), " Player ", [character], createdAt);

        snapshot.DisplayName.Should().Be("Player");
        snapshot.Characters.Should().ContainSingle().Which.Should().BeSameAs(character);
        snapshot.CreatedAtUtc.Should().Be(createdAt);
    }

    [Fact]
    public void Create_ShouldRejectInvalidPlayerId()
    {
        (() => RunPlayerSnapshot.Create(Guid.Empty, "Player", [Character("character.main")], DateTimeOffset.UtcNow))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectBlankName()
    {
        (() => RunPlayerSnapshot.Create(Guid.NewGuid(), " ", [Character("character.main")], DateTimeOffset.UtcNow))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectNullOrEmptyCharacters()
    {
        (() => RunPlayerSnapshot.Create(Guid.NewGuid(), "Player", null!, DateTimeOffset.UtcNow))
            .Should().Throw<DomainException>();
        (() => RunPlayerSnapshot.Create(Guid.NewGuid(), "Player", [], DateTimeOffset.UtcNow))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void DebugCharacterOperations_ShouldProtectProtagonistAndManageCompanion()
    {
        var protagonist = Character("character.main");
        var snapshot = RunPlayerSnapshot.Create(Guid.NewGuid(), "Player", [protagonist], DateTimeOffset.UtcNow);

        snapshot.DebugRemoveLastCompanion().Should().BeFalse();
        (() => snapshot.DebugAddCharacter(null!)).Should().Throw<ArgumentNullException>();

        var companion = Character("character.friend");
        snapshot.DebugAddCharacter(companion);
        snapshot.Characters.Should().HaveCount(2);
        snapshot.DebugRemoveLastCompanion().Should().BeTrue();
        snapshot.Characters.Should().ContainSingle().Which.Should().BeSameAs(protagonist);
    }

    [Fact]
    public void Rehydrate_ShouldRetainTrustedIdentity()
    {
        var id = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var character = Character("character.main");

        var snapshot = RunPlayerSnapshot.Rehydrate(
            id, playerId, "Persisted", DateTimeOffset.UnixEpoch, [character]);

        snapshot.Id.Should().Be(id);
        snapshot.PlayerId.Should().Be(playerId);
        snapshot.DisplayName.Should().Be("Persisted");
    }

    private static RunCharacterSnapshot Character(string key)
    {
        var stats = RunCharacterStatSnapshot.Create(
            100, 10, 5, 0, 10, 10, 5, 20, 0);
        var skill = RunCharacterSkillSnapshot.Create(
            "skill.test", "Skill", "Damage", "SingleEnemy", "Damage",
            0, 0, 10, emotionalRegister: "Neutral");
        return RunCharacterSnapshot.Create(
            Guid.NewGuid(), key, key, stats, [skill], emotionalRegisterCode: "Neutral");
    }
}
