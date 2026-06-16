using FluentAssertions;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.UnitTests.Selection;

public sealed class MarkovSelectionInfluenceTests
{
    [Fact]
    public void GetModifier_ShouldReturnOne_WhenNoModifier()
    {
        var influence = new MarkovSelectionInfluence("v1", "test", new Dictionary<string, decimal>());
        influence.GetModifier("unknown-key").Should().Be(1.0m);
    }

    [Fact]
    public void GetModifier_ShouldReturnModifier_WhenPresent()
    {
        var influence = new MarkovSelectionInfluence("v1", "test", new Dictionary<string, decimal>
        {
            ["enemy-a"] = 1.5m,
            ["enemy-b"] = 0.5m,
        });
        influence.GetModifier("enemy-a").Should().Be(1.5m);
        influence.GetModifier("enemy-b").Should().Be(0.5m);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMatrixVersionEmpty()
    {
        var act = () => new MarkovSelectionInfluence("", "test", new Dictionary<string, decimal>());
        act.Should().Throw<Leds.GameEngine.Domain.Common.DomainException>();
    }

    [Fact]
    public void Select_ShouldApplyMarkovInfluence_WhenProvided()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("boosted", 10),
            new SelectionCandidate("normal", 10),
        };
        var context = new SelectionContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "test-seed", "Test", null);

        var influence = new MarkovSelectionInfluence("v1", "test", new Dictionary<string, decimal>
        {
            ["boosted"] = 10.0m,
        });

        var selections = new Dictionary<string, int>();
        for (var i = 0; i < 100; i++)
        {
            var ctx = new SelectionContext(
                context.RunId, context.RoomId, context.NodeId,
                $"seed-{i}", context.DecisionType, context.ContextKey);
            var result = selector.Select(ctx, candidates, 1, influence);
            var key = result.Selected.First().Key;
            selections[key] = selections.GetValueOrDefault(key) + 1;
        }

        selections.GetValueOrDefault("boosted").Should().BeGreaterThan(selections.GetValueOrDefault("normal"));
    }

    [Fact]
    public void Select_ShouldPersistMatrixVersion_WhenMarkovInfluenceProvided()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[] { new SelectionCandidate("a", 10) };
        var context = new SelectionContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "test-seed", "Test", null);
        var influence = new MarkovSelectionInfluence("matrix-v2", "enemy-bias", new Dictionary<string, decimal>());

        var result = selector.Select(context, candidates, 1, influence);

        result.Decisions.First().MatrixVersion.Should().Be("matrix-v2");
        result.Decisions.First().InfluenceSummaryKey.Should().Be("enemy-bias");
    }

    [Fact]
    public void Select_ShouldNotPersistMatrixVersion_WhenNoInfluence()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[] { new SelectionCandidate("a", 10) };
        var context = new SelectionContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "test-seed", "Test", null);

        var result = selector.Select(context, candidates, 1);

        result.Decisions.First().MatrixVersion.Should().BeNull();
        result.Decisions.First().InfluenceSummaryKey.Should().BeNull();
    }

    [Fact]
    public void Select_ShouldExcludeCandidate_WhenMarkovInfluenceReducesWeightToZero()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("excluded", 10),
            new SelectionCandidate("kept", 10),
        };
        var context = new SelectionContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "test-seed", "Test", null);
        var influence = new MarkovSelectionInfluence("v1", "test", new Dictionary<string, decimal>
        {
            ["excluded"] = 0.0m,
        });

        var result = selector.Select(context, candidates, 1, influence);

        result.Selected.First().Key.Should().Be("kept");
    }

    [Fact]
    public void Select_ShouldFallback_WhenNoMarkovInfluence()
    {
        var selector = new DeterministicWeightedSelector();
        var candidates = new[]
        {
            new SelectionCandidate("a", 10),
            new SelectionCandidate("b", 10),
        };
        var context = new SelectionContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "test-seed", "Test", null);

        var result = selector.Select(context, candidates, 1);

        result.Selected.Should().HaveCount(1);
        result.Decisions.First().MatrixVersion.Should().BeNull();
    }
}
