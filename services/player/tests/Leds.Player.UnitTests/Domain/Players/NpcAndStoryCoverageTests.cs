using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Domain.Players;

public sealed class NpcAndStoryCoverageTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NpcReputationScore_Create_ShouldTrimKeyAndKeepValues()
    {
        var score = NpcReputationScore.Create("  npc.erika  ", 4, 2, "dialogue.1", Now);

        score.NpcKey.Should().Be("npc.erika");
        score.Score.Should().Be(4);
        score.TimesMet.Should().Be(2);
        score.CurrentDialogueNodeKey.Should().Be("dialogue.1");
        score.UpdatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NpcReputationScore_Create_ShouldRejectBlankKeys(string npcKey)
    {
        var act = () => NpcReputationScore.Create(npcKey, 0, 0, null, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void NpcReputationScore_ApplyDelta_ShouldReplaceDialogueNode_WhenProvided()
    {
        var score = NpcReputationScore.Create("npc.erika", 2, 1, "dialogue.old", Now.AddMinutes(-2));

        score.ApplyDelta(3, 2, "dialogue.new", Now);

        score.Score.Should().Be(5);
        score.TimesMet.Should().Be(3);
        score.CurrentDialogueNodeKey.Should().Be("dialogue.new");
        score.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void NpcReputationScore_ApplyDelta_ShouldPreserveDialogueNode_WhenNull()
    {
        var score = NpcReputationScore.Create("npc.erika", 2, 1, "dialogue.old", Now.AddMinutes(-2));

        score.ApplyDelta(-1, 1, null, Now);

        score.Score.Should().Be(1);
        score.TimesMet.Should().Be(2);
        score.CurrentDialogueNodeKey.Should().Be("dialogue.old");
    }

    [Fact]
    public void NpcReputationScore_Rehydrate_ShouldRestoreSnapshot()
    {
        var score = NpcReputationScore.Rehydrate("npc.majordome", -2, 7, null, Now);

        score.NpcKey.Should().Be("npc.majordome");
        score.Score.Should().Be(-2);
        score.TimesMet.Should().Be(7);
        score.CurrentDialogueNodeKey.Should().BeNull();
        score.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void MainStoryProgress_Advance_ShouldTrimValuesAndNormalizeBlankCheckpoint()
    {
        var progress = MainStoryProgress.CreateDefault();

        progress.Advance(" story.main ", " 1.0 ", " hall.entry ", "   ");

        progress.SequenceKey.Should().Be("story.main");
        progress.SequenceVersion.Should().Be("1.0");
        progress.StepKey.Should().Be("hall.entry");
        progress.CheckpointKey.Should().BeNull();
    }

    [Fact]
    public void MainStoryProgress_Advance_ShouldTrimNonBlankCheckpoint()
    {
        var progress = MainStoryProgress.CreateDefault();

        progress.Advance("story.main", "1.0", "hall.entry", " checkpoint.hall ");

        progress.CheckpointKey.Should().Be("checkpoint.hall");
    }

    [Theory]
    [InlineData("", "1", "step")]
    [InlineData("story", "", "step")]
    [InlineData("story", "1", "")]
    public void MainStoryProgress_Advance_ShouldRejectIncompleteIdentity(string sequence, string version, string step)
    {
        var progress = MainStoryProgress.CreateDefault();

        var act = () => progress.Advance(sequence, version, step, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_Advance_ShouldRejectCompletedStory()
    {
        var progress = MainStoryProgress.CreateDefault();
        progress.Complete();

        var act = () => progress.Advance("story", "1", "step", null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_UnlockRoom_ShouldAddOnceAndRejectBlankKey()
    {
        var progress = MainStoryProgress.CreateDefault();

        progress.UnlockRoom(" room.hall ").Should().BeTrue();
        progress.UnlockRoom("ROOM.HALL").Should().BeFalse();
        progress.UnlockedRoomKeys.Should().ContainSingle().Which.Should().Be("room.hall");
        var act = () => progress.UnlockRoom("  ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_RevealRoom_ShouldAddOnceAndRejectBlankKey()
    {
        var progress = MainStoryProgress.CreateDefault();

        progress.RevealRoom(" room.hospital ").Should().BeTrue();
        progress.RevealRoom("ROOM.HOSPITAL").Should().BeFalse();
        progress.VisibleRoomKeys.Should().ContainSingle().Which.Should().Be("room.hospital");
        var act = () => progress.RevealRoom("");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_Complete_ShouldUnlockFirstDifficultyButNeverDowngrade()
    {
        var fresh = MainStoryProgress.CreateDefault();
        fresh.Complete();
        fresh.HighestDifficultyLevelUnlocked.Should().Be(1);

        var advanced = MainStoryProgress.Rehydrate("story", "1", "end", null, false, 3, [], []);
        advanced.Complete();
        advanced.HighestDifficultyLevelUnlocked.Should().Be(3);
    }

    [Fact]
    public void MainStoryProgress_UnlockNextDifficulty_ShouldRequireCompletedStory()
    {
        var progress = MainStoryProgress.CreateDefault();

        var act = () => progress.UnlockNextDifficulty(1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_UnlockNextDifficulty_ShouldIgnoreAlreadyUnlockedLevel()
    {
        var progress = MainStoryProgress.CreateDefault();
        progress.Complete();

        progress.UnlockNextDifficulty(1).Should().BeFalse();
        progress.HighestDifficultyLevelUnlocked.Should().Be(1);
    }

    [Fact]
    public void MainStoryProgress_UnlockNextDifficulty_ShouldRejectSkippedLevel()
    {
        var progress = MainStoryProgress.CreateDefault();
        progress.Complete();

        var act = () => progress.UnlockNextDifficulty(3);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MainStoryProgress_UnlockNextDifficulty_ShouldUnlockSequentialLevel()
    {
        var progress = MainStoryProgress.CreateDefault();
        progress.Complete();

        progress.UnlockNextDifficulty(2).Should().BeTrue();
        progress.HighestDifficultyLevelUnlocked.Should().Be(2);
    }

    [Fact]
    public void PlayerProfile_AdvanceMainStory_ShouldBecomeNoOpAfterCompletion()
    {
        var profile = PlayerProfile.Create("Story Player", Now.AddHours(-1));
        profile.AdvanceMainStory("story", "1", "end", null, ["room.one"], ["room.one"], true, Now.AddMinutes(-1));
        var completedAt = profile.UpdatedAtUtc;

        profile.AdvanceMainStory("other", "2", "ignored", null, ["room.two"], ["room.two"], false, Now);

        profile.MainStoryProgress.SequenceKey.Should().Be("story");
        profile.MainStoryProgress.UnlockedRoomKeys.Should().NotContain("room.two");
        profile.UpdatedAtUtc.Should().Be(completedAt);
    }

    [Fact]
    public void PlayerProfile_UpsertNpcReputationScores_ShouldAddThenUpdateSameNpc()
    {
        var profile = PlayerProfile.Create("Reputation Player", Now.AddHours(-1));
        var first = NpcReputationScore.Create("npc.erika", 2, 1, "dialogue.1", Now.AddMinutes(-2));
        var update = NpcReputationScore.Create("NPC.ERIKA", 7, 4, "dialogue.2", Now.AddMinutes(-1));

        profile.UpsertNpcReputationScores([first], Now.AddMinutes(-2));
        profile.UpsertNpcReputationScores([update], Now);

        profile.NpcReputationScores.Should().ContainSingle();
        var stored = profile.GetNpcReputationScore("npc.erika");
        stored.Should().NotBeNull();
        stored!.Score.Should().Be(7);
        stored.TimesMet.Should().Be(4);
        stored.CurrentDialogueNodeKey.Should().Be("dialogue.2");
        profile.GetNpcReputationScore("npc.unknown").Should().BeNull();
    }

    [Fact]
    public void PlayerProfile_UnlockDifficultyLevel_ShouldOnlyTouchWhenLevelActuallyUnlocks()
    {
        var profile = PlayerProfile.Create("Difficulty Player", Now.AddHours(-1));
        profile.AdvanceMainStory("story", "1", "end", null, [], [], true, Now.AddMinutes(-2));
        profile.UnlockDifficultyLevel(2, Now.AddMinutes(-1));
        var afterUnlock = profile.UpdatedAtUtc;

        profile.UnlockDifficultyLevel(2, Now);

        profile.MainStoryProgress.HighestDifficultyLevelUnlocked.Should().Be(2);
        profile.UpdatedAtUtc.Should().Be(afterUnlock);
    }
}
