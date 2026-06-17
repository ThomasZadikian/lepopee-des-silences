using FluentAssertions;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolverCatalogTests
{
    private static LawEventChoiceResolver CreateResolver()
    {
        return new LawEventChoiceResolver(new InMemoryCatalogContentGateway());
    }

    private static CurrentEventChoiceResolutionContext CreateLawContext(
        Run? run = null,
        string choiceId = "accept-law:law-aegis-v1")
    {
        run ??= TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var room = run.CurrentRoom;
        var node = room.Nodes.First(n => n.EventType == NodeEventType.Law);

        return new CurrentEventChoiceResolutionContext(run, room, node, choiceId);
    }

    [Fact]
    public async Task AcceptLaw_ShouldCreateActivePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        await resolver.ResolveAsync(context);

        run.ActivePalaceLaws.Should().HaveCount(1);
        run.ActivePalaceLaws.First().Key.Should().Be("law-aegis-v1");
    }

    [Fact]
    public async Task AcceptLaw_ShouldCreateRunModifiersWithSourceTypePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        await resolver.ResolveAsync(context);

        var modifiers = run.RunModifiers.ToList();
        modifiers.Should().NotBeEmpty();
        modifiers.Should().AllSatisfy(m => m.SourceType.Should().Be("PalaceLaw"));
        modifiers.Should().AllSatisfy(m => m.SourceKey.Should().Be("law-aegis-v1"));
    }

    [Fact]
    public async Task AcceptLaw_ShouldSnapshotLawDisplayData()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        await resolver.ResolveAsync(context);

        var law = run.ActivePalaceLaws.First();
        law.DisplayName.Should().Be("Loi de l'Égide");
        law.Duration.Should().Be("UntilRunEnds");
        law.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AcceptLaw_ShouldNotApplySameLawTwice()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        await resolver.ResolveAsync(context);
        await resolver.ResolveAsync(context);

        run.ActivePalaceLaws.Should().HaveCount(1);
    }

    [Fact]
    public async Task AcceptLaw_ShouldApplyUntilRunEndsDuration()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        await resolver.ResolveAsync(context);

        run.RunModifiers.Should().AllSatisfy(m =>
            m.Duration.Should().Be(RunModifierDuration.UntilRunEnds));
    }

    [Fact]
    public async Task AcceptLaw_ShouldReturnAcceptanceNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run);

        var result = await resolver.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RejectLaw_ShouldNotCreateActivePalaceLaw()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run, "reject-law:law-aegis-v1");

        await resolver.ResolveAsync(context);

        run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public async Task RejectLaw_ShouldReturnRejectionNarrative()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Law);
        var resolver = CreateResolver();
        var context = CreateLawContext(run, "reject-law:law-aegis-v1");

        var result = await resolver.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotBeEmpty();
    }
}
