using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class CurseEventChoiceResolverCatalogTests
{
    private static CurseEventChoiceResolver CreateResolver()
    {
        return new CurseEventChoiceResolver(
            Mock.Of<ICatalogCurseDefinitionProvider>(),
            Mock.Of<ICatalogEffectSetProvider>());
    }

    private static CurrentEventChoiceResolutionContext CreateCurseContext(
        Run? run = null,
        string choiceId = "accept-curse")
    {
        run ??= TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var room = run.CurrentRoom;
        var node = room.Nodes.First(n => n.EventType == NodeEventType.Curse);

        return new CurrentEventChoiceResolutionContext(run, room, node, choiceId);
    }

    [Fact]
    public void AcceptCurse_ShouldCreateActiveCurse()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        resolver.Resolve(context);

        run.ActiveCurse.Should().NotBeNull();
        run.ActiveCurse!.Key.Should().StartWith("curse.");
    }

    [Fact]
    public void AcceptCurse_ShouldCreateRunModifierWithSourceTypeCurse()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        resolver.Resolve(context);

        var curseModifiers = run.RunModifiers
            .Where(m => m.SourceType == "Curse")
            .ToList();
        curseModifiers.Should().NotBeEmpty();
        curseModifiers.Should().AllSatisfy(m =>
            m.SourceKey.Should().StartWith("curse."));
    }

    [Fact]
    public void AcceptCurse_ShouldSnapshotCurseDisplayData()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        resolver.Resolve(context);

        var curse = run.ActiveCurse!;
        curse.DisplayName.Should().NotBeNullOrEmpty();
        curse.Duration.Should().NotBeNullOrEmpty();
        curse.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AcceptCurse_ShouldApplyNextCombatOnlyDuration()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        resolver.Resolve(context);

        var curseModifiers = run.RunModifiers
            .Where(m => m.SourceType == "Curse")
            .ToList();
        curseModifiers.Should().AllSatisfy(m =>
            m.Duration.Should().Be(RunModifierDuration.NextCombatOnly));
    }

    [Fact]
    public void AcceptCurse_ShouldReturnAcceptanceNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        var result = resolver.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public void RejectCurse_ShouldNotCreateActiveCurse()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run, "reject-curse");

        resolver.Resolve(context);

        run.ActiveCurse.Should().BeNull();
    }

    [Fact]
    public void RejectCurse_ShouldReturnRejectionNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run, "reject-curse");

        var result = resolver.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public void DifficultyCurse_ShouldAffectNextCombatDifficultyMultiplier()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Curse);
        var resolver = CreateResolver();
        var context = CreateCurseContext(run);

        resolver.Resolve(context);

        var difficultyModifiers = run.GetActiveModifiers(RunModifierType.NextCombatDifficultyMultiplier);
        difficultyModifiers.Should().NotBeEmpty();
        difficultyModifiers.Should().AllSatisfy(m =>
            m.SourceType.Should().Be("Curse"));
    }
}
