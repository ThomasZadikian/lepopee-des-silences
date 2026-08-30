using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Players;

public sealed class MainStoryProgressTests
{
    [Fact]
    public void RoomUnlocks_ShouldBePermanentAndIdempotent()
    {
        var progress = MainStoryProgress.CreateDefault();

        progress.UnlockRoom("room.hall").Should().BeTrue();
        progress.UnlockRoom("room.hall").Should().BeFalse();
        progress.UnlockedRoomKeys.Should().ContainSingle("room.hall");
    }

    [Fact]
    public void DifficultyN_ShouldStayLockedUntilStoryCompletion_AndUnlockSequentially()
    {
        var progress = MainStoryProgress.CreateDefault();

        var beforeStory = () => progress.UnlockNextDifficulty(1);
        beforeStory.Should().Throw<DomainException>();

        progress.Complete();
        progress.HighestDifficultyLevelUnlocked.Should().Be(1);
        progress.UnlockNextDifficulty(2).Should().BeTrue();
        progress.UnlockNextDifficulty(2).Should().BeFalse();

        var skipMastery = () => progress.UnlockNextDifficulty(4);
        skipMastery.Should().Throw<DomainException>();
    }
}
