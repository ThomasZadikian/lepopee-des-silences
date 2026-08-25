using FluentAssertions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class RunProgressionSelectionPolicyTests
{
    [Fact]
    public void IncompleteMainStory_ShouldForceStoryMode()
    {
        var selection = RunProgressionSelectionPolicy.Resolve(MainStoryProgressView.Incomplete, null);

        selection.Mode.Should().Be(RunProgressionMode.Story);
        selection.DifficultyLevel.Should().BeNull();
        selection.StoryOverlay.Should().NotBeNull();
    }

    [Fact]
    public void IncompleteMainStory_ShouldRejectDifficultyN()
    {
        var act = () => RunProgressionSelectionPolicy.Resolve(MainStoryProgressView.Incomplete, 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompletedMainStory_ShouldAllowOnlyMasteredDifficulty()
    {
        var completed = new MainStoryProgressView(
            null, null, null, null, true, 2, [], []);

        RunProgressionSelectionPolicy.Resolve(completed, 2).DifficultyLevel!.Value.Value.Should().Be(2);
        var locked = () => RunProgressionSelectionPolicy.Resolve(completed, 3);
        locked.Should().Throw<DomainException>();
    }
}
