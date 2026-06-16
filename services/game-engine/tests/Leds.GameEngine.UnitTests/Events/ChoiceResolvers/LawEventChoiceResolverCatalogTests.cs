using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolverCatalogTests
{
    private static LawEventChoiceResolver CreateResolver(
        IPalaceLawCatalog? catalog = null)
    {
        return new LawEventChoiceResolver(
            catalog ?? new StaticPalaceLawCatalog(),
            Mock.Of<ICatalogPalaceLawDefinitionProvider>(),
            Mock.Of<ICatalogEffectSetProvider>());
    }

    private static CurrentEventChoiceResolutionContext CreateLawContext(
        Run? run = null,
        string choiceId = "accept-law")
    {
        run ??= TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var room = run.CurrentRoom;
        var node = room.Nodes.First(n => n.EventType == NodeEventType.Law);

        return new CurrentEventChoiceResolutionContext(run, room, node, choiceId);
    }

    [Fact]
    public void AcceptLaw_ShouldCreateActivePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        resolver.Resolve(context);

        run.ActivePalaceLaws.Should().HaveCount(1);
        run.ActivePalaceLaws.First().Key.Should().Be("law-silence-v1");
    }

    [Fact]
    public void AcceptLaw_ShouldCreateRunModifiersWithSourceTypePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        resolver.Resolve(context);

        var modifiers = run.RunModifiers.ToList();
        modifiers.Should().NotBeEmpty();
        modifiers.Should().AllSatisfy(m => m.SourceType.Should().Be("PalaceLaw"));
        modifiers.Should().AllSatisfy(m => m.SourceKey.Should().Be("law-silence-v1"));
    }

    [Fact]
    public void AcceptLaw_ShouldSnapshotLawDisplayData()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        resolver.Resolve(context);

        var law = run.ActivePalaceLaws.First();
        law.DisplayName.Should().Be("Loi du Silence");
        law.Duration.Should().Be("UntilRunEnds");
        law.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AcceptLaw_ShouldNotApplySameLawTwice()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        resolver.Resolve(context);
        resolver.Resolve(context);

        run.ActivePalaceLaws.Should().HaveCount(1);
    }

    [Fact]
    public void AcceptLaw_ShouldApplyUntilRunEndsDuration()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        resolver.Resolve(context);

        run.RunModifiers.Should().AllSatisfy(m =>
            m.Duration.Should().Be(RunModifierDuration.UntilRunEnds));
    }

    [Fact]
    public void AcceptLaw_ShouldReturnAcceptanceNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        var result = resolver.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public void RejectLaw_ShouldNotCreateActivePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run, "reject-law");

        resolver.Resolve(context);

        run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public void RejectLaw_ShouldReturnRejectionNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run, "reject-law");

        var result = resolver.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }
}
