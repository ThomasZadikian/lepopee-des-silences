using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed record RunProgressionSelection(
    RunProgressionMode Mode,
    DifficultyLevel? DifficultyLevel,
    StoryRunOverlay? StoryOverlay);

public static class RunProgressionSelectionPolicy
{
    public static RunProgressionSelection Resolve(
        MainStoryProgressView? progress,
        int? requestedDifficultyLevel)
    {
        var mainStory = progress ?? MainStoryProgressView.Incomplete;
        if (!mainStory.IsCompleted)
        {
            if (requestedDifficultyLevel.HasValue)
                throw new DomainException("Difficulty N is unavailable until the Main Story is completed.");

            return new RunProgressionSelection(
                RunProgressionMode.Story,
                null,
                new StoryRunOverlay(
                    mainStory.SequenceKey,
                    mainStory.SequenceVersion,
                    mainStory.StepKey,
                    mainStory.CheckpointKey));
        }

        var difficulty = DifficultyLevel.Create(requestedDifficultyLevel ?? 1);
        if (difficulty.Value > mainStory.HighestDifficultyLevelUnlocked)
            throw new DomainException($"Difficulty {difficulty.Value} has not been unlocked through mastery.");

        return new RunProgressionSelection(RunProgressionMode.Standard, difficulty, null);
    }
}
