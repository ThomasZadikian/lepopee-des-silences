using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.UnitTests.Selection;

public sealed class SelectionDecisionTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var runId = Guid.NewGuid();
        var decision = SelectionDecision.Create(
            runId,
            "room-type",
            "markov-selection",
            "Combat",
            "room-generation",
            "seed-123",
            "deterministic-v2");

        decision.Id.Should().NotBeEmpty();
        decision.RunId.Should().Be(runId);
        decision.DecisionType.Should().Be("room-type");
        decision.ContextKey.Should().Be("markov-selection");
        decision.SelectedKey.Should().Be("Combat");
        decision.SelectionGroup.Should().Be("room-generation");
        decision.Seed.Should().Be("seed-123");
        decision.AlgorithmVersion.Should().Be("deterministic-v2");
        decision.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        decision.MatrixVersion.Should().BeNull();
        decision.InfluenceSummaryKey.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimAllStringFields()
    {
        var decision = SelectionDecision.Create(
            Guid.NewGuid(),
            "  room-type  ",
            "  context  ",
            "  Combat  ",
            "  group  ",
            "  seed  ",
            "  v2  ");

        decision.DecisionType.Should().Be("room-type");
        decision.ContextKey.Should().Be("context");
        decision.SelectedKey.Should().Be("Combat");
        decision.SelectionGroup.Should().Be("group");
        decision.Seed.Should().Be("seed");
        decision.AlgorithmVersion.Should().Be("v2");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDecisionTypeIsNullOrWhitespace()
    {
        var act1 = () => SelectionDecision.Create(Guid.NewGuid(), "", "ctx", "sel", "grp", "seed", "v2");
        var act2 = () => SelectionDecision.Create(Guid.NewGuid(), "   ", "ctx", "sel", "grp", "seed", "v2");

        act1.Should().Throw<DomainException>().WithMessage("Selection decision type is required.");
        act2.Should().Throw<DomainException>().WithMessage("Selection decision type is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSelectedKeyIsNullOrWhitespace()
    {
        var act1 = () => SelectionDecision.Create(Guid.NewGuid(), "type", "ctx", "", "grp", "seed", "v2");
        var act2 = () => SelectionDecision.Create(Guid.NewGuid(), "type", "ctx", "   ", "grp", "seed", "v2");

        act1.Should().Throw<DomainException>().WithMessage("Selection decision selected key is required.");
        act2.Should().Throw<DomainException>().WithMessage("Selection decision selected key is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenSeedIsNullOrWhitespace()
    {
        var act1 = () => SelectionDecision.Create(Guid.NewGuid(), "type", "ctx", "sel", "grp", "", "v2");
        var act2 = () => SelectionDecision.Create(Guid.NewGuid(), "type", "ctx", "sel", "grp", "   ", "v2");

        act1.Should().Throw<DomainException>().WithMessage("Selection decision seed is required.");
        act2.Should().Throw<DomainException>().WithMessage("Selection decision seed is required.");
    }

    [Fact]
    public void Create_ShouldAcceptOptionalMatrixVersion()
    {
        var decision = SelectionDecision.Create(
            Guid.NewGuid(),
            "room-type",
            null,
            "Combat",
            null,
            "seed",
            "v2",
            matrixVersion: "markov-1.0");

        decision.MatrixVersion.Should().Be("markov-1.0");
    }

    [Fact]
    public void Create_ShouldAcceptOptionalInfluenceSummaryKey()
    {
        var decision = SelectionDecision.Create(
            Guid.NewGuid(),
            "room-type",
            null,
            "Combat",
            null,
            "seed",
            "v2",
            influenceSummaryKey: "psyche-anxious");

        decision.InfluenceSummaryKey.Should().Be("psyche-anxious");
    }

    [Fact]
    public void Create_ShouldAcceptNullContextKeyAndSelectionGroup()
    {
        var decision = SelectionDecision.Create(
            Guid.NewGuid(),
            "room-type",
            null,
            "Combat",
            null,
            "seed",
            "v2");

        decision.ContextKey.Should().BeNull();
        decision.SelectionGroup.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreDecision_WithGivenValues()
    {
        var id = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);

        var decision = SelectionDecision.Rehydrate(
            id,
            runId,
            "room-type",
            "context",
            "Combat",
            "group",
            "seed",
            "v2",
            createdAt,
            matrixVersion: "markov-1.0",
            influenceSummaryKey: "psyche-calm");

        decision.Id.Should().Be(id);
        decision.RunId.Should().Be(runId);
        decision.DecisionType.Should().Be("room-type");
        decision.ContextKey.Should().Be("context");
        decision.SelectedKey.Should().Be("Combat");
        decision.SelectionGroup.Should().Be("group");
        decision.Seed.Should().Be("seed");
        decision.AlgorithmVersion.Should().Be("v2");
        decision.CreatedAtUtc.Should().Be(createdAt);
        decision.MatrixVersion.Should().Be("markov-1.0");
        decision.InfluenceSummaryKey.Should().Be("psyche-calm");
    }
}
