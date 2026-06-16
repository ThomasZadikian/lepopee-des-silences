using FluentAssertions;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.UnitTests.Selection;

public sealed class DeterministicWeightedSelectorTests
{
    private static SelectionContext CreateContext(
        string seed = "test-seed", string decisionType = "TestSelection") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), seed, decisionType, null);

    [Fact]
    public void Select_ShouldReturnEmpty_WhenNoCandidates()
    {
        var selector = new DeterministicWeightedSelector();
        var result = selector.Select(CreateContext(), []);
        result.Selected.Should().BeEmpty();
        result.Decisions.Should().BeEmpty();
    }

    [Fact]
    public void Select_ShouldReturnEmpty_WhenAllWeightsZero()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("a", 0),
            new SelectionCandidate("b", 0),
        };
        var result = selector.Select(CreateContext(), candidates);
        result.Selected.Should().BeEmpty();
    }

    [Fact]
    public void Select_ShouldSelectSingleCandidate()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[] { new SelectionCandidate("only-one", 10) };
        var result = selector.Select(CreateContext(), candidates, maxSelections: 1);
        result.Selected.Should().HaveCount(1);
        result.Selected.First().Key.Should().Be("only-one");
    }

    [Fact]
    public void Select_ShouldBeDeterministic_ForSameSeed()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("a", 10),
            new SelectionCandidate("b", 5),
            new SelectionCandidate("c", 3),
        };
        var context = CreateContext(seed: "deterministic-seed");

        var result1 = selector.Select(context, candidates, maxSelections: 2);
        var result2 = selector.Select(context, candidates, maxSelections: 2);

        result1.Selected.Select(r => r.Key).Should().Equal(result2.Selected.Select(r => r.Key));
    }

    [Fact]
    public void Select_ShouldSelectDifferent_ForDifferentSeed()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = Enumerable.Range(0, 20)
            .Select(i => new SelectionCandidate($"candidate{i}", 10))
            .ToArray();

        var result1 = selector.Select(CreateContext(seed: "seed-a"), candidates, maxSelections: 5);
        var result2 = selector.Select(CreateContext(seed: "seed-b"), candidates, maxSelections: 5);

        result1.Selected.Select(r => r.Key).Should().NotEqual(result2.Selected.Select(r => r.Key));
    }

    [Fact]
    public void Select_ShouldRespectMaxSelections()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = Enumerable.Range(0, 10)
            .Select(i => new SelectionCandidate($"c{i}", 10))
            .ToArray();

        var result = selector.Select(CreateContext(), candidates, maxSelections: 3);
        result.Selected.Should().HaveCount(3);
    }

    [Fact]
    public void Select_ShouldNotExceedCandidateCount()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("a", 10),
            new SelectionCandidate("b", 10),
        };

        var result = selector.Select(CreateContext(), candidates, maxSelections: 5);
        result.Selected.Should().HaveCount(2);
    }

    [Fact]
    public void Select_ShouldNotSelectSameCandidateTwice()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("a", 10),
            new SelectionCandidate("b", 10),
        };

        var result = selector.Select(CreateContext(), candidates, maxSelections: 2);
        result.Selected.Select(r => r.Key).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public void Select_ShouldRespectWeights()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("heavy", 1000),
            new SelectionCandidate("light", 1),
        };
        var context = CreateContext();

        var selections = new Dictionary<string, int>();
        for (var i = 0; i < 100; i++)
        {
            var ctx = new SelectionContext(
                context.RunId, context.RoomId, context.NodeId,
                $"seed-{i}", context.DecisionType, context.ContextKey);
            var result = selector.Select(ctx, candidates, maxSelections: 1);
            var key = result.Selected.First().Key;
            selections[key] = selections.GetValueOrDefault(key) + 1;
        }

        selections.GetValueOrDefault("heavy").Should().BeGreaterThan(selections.GetValueOrDefault("light"));
    }

    [Fact]
    public void Select_ShouldCreateDecisions()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[] { new SelectionCandidate("a", 10) };
        var context = CreateContext(decisionType: "EnemySelection");

        var result = selector.Select(context, candidates, maxSelections: 1);

        result.Decisions.Should().HaveCount(1);
        var decision = result.Decisions.First();
        decision.DecisionType.Should().Be("EnemySelection");
        decision.SelectedKey.Should().Be("a");
        decision.RunId.Should().Be(context.RunId);
        decision.Seed.Should().Be(context.Seed);
        decision.AlgorithmVersion.Should().Be("deterministic-v1");
    }

    [Fact]
    public void Select_ShouldSetSelectionGroup_OnDecision()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[] { new SelectionCandidate("a", 10, "Elite") };
        var context = CreateContext();

        var result = selector.Select(context, candidates, maxSelections: 1);

        result.Decisions.First().SelectionGroup.Should().Be("Elite");
    }

    [Fact]
    public void SelectionCandidate_ShouldThrow_WhenKeyEmpty()
    {
        var act = () => new SelectionCandidate("", 10);
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }

    [Fact]
    public void SelectionCandidate_ShouldThrow_WhenWeightNegative()
    {
        var act = () => new SelectionCandidate("a", -1);
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }

    [Fact]
    public void SelectionDecision_Create_ShouldThrow_WhenDecisionTypeEmpty()
    {
        var act = () => SelectionDecision.Create(Guid.NewGuid(), "", null, "key", null, "seed", "v1");
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }

    [Fact]
    public void SelectionDecision_Create_ShouldThrow_WhenSelectedKeyEmpty()
    {
        var act = () => SelectionDecision.Create(Guid.NewGuid(), "type", null, "", null, "seed", "v1");
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }
}
