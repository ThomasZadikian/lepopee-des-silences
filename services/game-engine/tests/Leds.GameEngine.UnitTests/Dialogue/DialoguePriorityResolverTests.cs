using FluentAssertions;
using Leds.GameEngine.Domain.Dialogue;

namespace Leds.GameEngine.UnitTests.Dialogue;

public sealed class DialoguePriorityResolverTests
{
    [Fact]
    public void SelectHighestPriority_ShouldReturnNull_WhenNoCandidates()
    {
        DialoguePriorityResolver.SelectHighestPriority([]).Should().BeNull();
    }

    [Fact]
    public void SelectHighestPriority_ShouldPreferScripted_OverEverythingElse()
    {
        var candidates = new[]
        {
            new DialogueCandidate("ambient.a", DialoguePriority.Ambient),
            new DialogueCandidate("scripted.a", DialoguePriority.Scripted),
            new DialogueCandidate("critical.a", DialoguePriority.CriticalEvent),
        };

        DialoguePriorityResolver.SelectHighestPriority(candidates)!.Key.Should().Be("scripted.a");
    }

    [Fact]
    public void SelectHighestPriority_ShouldRespectFullOrdering()
    {
        var candidates = new[]
        {
            new DialogueCandidate("ambient.a", DialoguePriority.Ambient),
            new DialogueCandidate("contextual.a", DialoguePriority.Contextual),
            new DialogueCandidate("urgent.a", DialoguePriority.UrgentReaction),
        };

        DialoguePriorityResolver.SelectHighestPriority(candidates)!.Key.Should().Be("urgent.a");
    }

    [Fact]
    public void SelectHighestPriority_ShouldKeepEarliestCandidate_OnATie()
    {
        var candidates = new[]
        {
            new DialogueCandidate("ambient.first", DialoguePriority.Ambient),
            new DialogueCandidate("ambient.second", DialoguePriority.Ambient),
        };

        DialoguePriorityResolver.SelectHighestPriority(candidates)!.Key.Should().Be("ambient.first");
    }
}
