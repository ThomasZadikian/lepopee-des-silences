using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players.AdvanceMainStory;
using Leds.Player.Application.Players.AwardCurrency;
using Leds.Player.Application.Players.AwardHimLitCurrency;
using Leds.Player.Application.Players.EquipSkill;
using Leds.Player.Application.Players.GetNpcReputationScores;
using Leds.Player.Application.Players.UnequipSkill;
using Leds.Player.Application.Players.UnlockDifficultyLevel;
using Leds.Player.Application.Players.UnlockSkill;
using Leds.Player.Application.Players.UpsertNpcReputationScores;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class PlayerApplicationCoverageTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AdvanceMainStory_ShouldPersistAndProjectStoryProgress()
    {
        var (profile, repository) = CreateProfileFixture();
        var handler = new AdvanceMainStoryCommandHandler(repository.Object, new FixedTimeProvider(Now));
        var command = new AdvanceMainStoryCommand(
            profile.Id.Value,
            "story.main",
            "1.0",
            "hall.entry",
            "checkpoint.hall",
            ["room.hall"],
            ["room.hall", "room.hospital"],
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MainStory.SequenceKey.Should().Be("story.main");
        result.MainStory.SequenceVersion.Should().Be("1.0");
        result.MainStory.StepKey.Should().Be("hall.entry");
        result.MainStory.CheckpointKey.Should().Be("checkpoint.hall");
        result.MainStory.IsCompleted.Should().BeTrue();
        result.MainStory.UnlockedRoomKeys.Should().Contain("room.hall");
        result.MainStory.VisibleRoomKeys.Should().Contain(["room.hall", "room.hospital"]);
        result.UpdatedAtUtc.Should().Be(Now);
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceMainStory_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new AdvanceMainStoryCommandHandler(repository.Object, new FixedTimeProvider(Now));
        var command = new AdvanceMainStoryCommand(Guid.NewGuid(), "story", "1", "step", null, [], [], false);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AwardCurrency_ShouldPersistPalaceShards()
    {
        var (profile, repository) = CreateProfileFixture();
        var handler = new AwardCurrencyCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(new AwardCurrencyCommand(profile.Id.Value, 7), CancellationToken.None);

        result.Progression.PalaceShardCount.Should().Be(7);
        result.UpdatedAtUtc.Should().Be(Now);
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardCurrency_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new AwardCurrencyCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(new AwardCurrencyCommand(Guid.NewGuid(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AwardHimLitCurrency_ShouldPersistHimLitShards()
    {
        var (profile, repository) = CreateProfileFixture();
        var handler = new AwardHimLitCurrencyCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(new AwardHimLitCurrencyCommand(profile.Id.Value, 5), CancellationToken.None);

        result.Progression.HimLitShardCount.Should().Be(5);
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardHimLitCurrency_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new AwardHimLitCurrencyCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(new AwardHimLitCurrencyCommand(Guid.NewGuid(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnlockSkill_ShouldLearnSkillAndPersist()
    {
        var (profile, repository) = CreateProfileFixture();
        var characterId = profile.Roster.Characters.Single().Id;
        var handler = new UnlockSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new UnlockSkillCommand(profile.Id.Value, characterId.Value, "skill.coverage", "test"),
            CancellationToken.None);

        result.Characters.Single().Skills.Should().Contain(s =>
            s.SkillKey == "skill.coverage" && s.Source == "test");
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockSkill_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new UnlockSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(
            new UnlockSkillCommand(Guid.NewGuid(), Guid.NewGuid(), "skill.coverage", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task EquipSkill_ShouldEquipLearnedSkill()
    {
        var (profile, repository) = CreateProfileFixture();
        var characterId = profile.Roster.Characters.Single().Id;
        profile.LearnSkill(characterId, "skill.coverage", "test", Now.AddMinutes(-2));
        var handler = new EquipSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new EquipSkillCommand(profile.Id.Value, characterId.Value, "skill.coverage"),
            CancellationToken.None);

        result.Characters.Single().Skills.Single(s => s.SkillKey == "skill.coverage").IsEquipped.Should().BeTrue();
    }

    [Fact]
    public async Task EquipSkill_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new EquipSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(
            new EquipSkillCommand(Guid.NewGuid(), Guid.NewGuid(), "skill.coverage"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnequipSkill_ShouldUnequipEquippedSkill()
    {
        var (profile, repository) = CreateProfileFixture();
        var characterId = profile.Roster.Characters.Single().Id;
        profile.LearnSkill(characterId, "skill.coverage", "test", Now.AddMinutes(-3));
        profile.EquipSkill(characterId, "skill.coverage", Now.AddMinutes(-2));
        var handler = new UnequipSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new UnequipSkillCommand(profile.Id.Value, characterId.Value, "skill.coverage"),
            CancellationToken.None);

        result.Characters.Single().Skills.Single(s => s.SkillKey == "skill.coverage").IsEquipped.Should().BeFalse();
    }

    [Fact]
    public async Task UnequipSkill_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new UnequipSkillCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(
            new UnequipSkillCommand(Guid.NewGuid(), Guid.NewGuid(), "skill.coverage"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnlockDifficultyLevel_ShouldUnlockNextLevelAfterMainStory()
    {
        var (profile, repository) = CreateProfileFixture();
        profile.AdvanceMainStory("story", "1", "final", null, [], [], true, Now.AddMinutes(-1));
        var handler = new UnlockDifficultyLevelCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new UnlockDifficultyLevelCommand(profile.Id.Value, 2),
            CancellationToken.None);

        result.MainStory.HighestDifficultyLevelUnlocked.Should().Be(2);
        result.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task UnlockDifficultyLevel_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new UnlockDifficultyLevelCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(new UnlockDifficultyLevelCommand(Guid.NewGuid(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpsertNpcReputationScores_ShouldAddAndProjectScore()
    {
        var (profile, repository) = CreateProfileFixture();
        var handler = new UpsertNpcReputationScoresCommandHandler(repository.Object, new FixedTimeProvider(Now));
        var command = new UpsertNpcReputationScoresCommand(
            profile.Id.Value,
            Guid.NewGuid(),
            [new NpcReputationScoreDto("npc.majordome", 4, 1, "dialogue.2")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().NpcKey.Should().Be("npc.majordome");
        result.Single().Score.Should().Be(4);
        result.Single().TimesMet.Should().Be(1);
        result.Single().CurrentDialogueNodeKey.Should().Be("dialogue.2");
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertNpcReputationScores_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new UpsertNpcReputationScoresCommandHandler(repository.Object, new FixedTimeProvider(Now));

        var act = () => handler.Handle(
            new UpsertNpcReputationScoresCommand(Guid.NewGuid(), Guid.NewGuid(), []), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetNpcReputationScores_ShouldProjectStoredScores()
    {
        var (profile, repository) = CreateProfileFixture();
        profile.UpsertNpcReputationScores(
            [NpcReputationScore.Create("npc.erika", 3, 2, "dialogue.erika", Now.AddMinutes(-1))],
            Now.AddMinutes(-1));
        var handler = new GetNpcReputationScoresQueryHandler(repository.Object);

        var result = await handler.Handle(new GetNpcReputationScoresQuery(profile.Id.Value), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().NpcKey.Should().Be("npc.erika");
        result.Single().Score.Should().Be(3);
    }

    [Fact]
    public async Task GetNpcReputationScores_ShouldThrow_WhenPlayerDoesNotExist()
    {
        var repository = MissingProfileRepository();
        var handler = new GetNpcReputationScoresQueryHandler(repository.Object);

        var act = () => handler.Handle(new GetNpcReputationScoresQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static (PlayerProfile Profile, Mock<IPlayerProfileRepository> Repository) CreateProfileFixture()
    {
        var profile = PlayerProfile.Create("Coverage Player", Now.AddHours(-1));
        profile.CreatePlayableCharacter("L'Aventurier", "archetype.porteur", Now.AddHours(-1));
        var repository = new Mock<IPlayerProfileRepository>();
        repository
            .Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repository
            .Setup(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (profile, repository);
    }

    private static Mock<IPlayerProfileRepository> MissingProfileRepository()
    {
        var repository = new Mock<IPlayerProfileRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);
        return repository;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
