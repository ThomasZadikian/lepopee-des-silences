using FluentAssertions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolverTests
{
    [Fact]
    public void Resolve_ShouldActivatePalaceLaw_WhenChoiceIsAcceptLaw()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law");

        var sut = new LawEventChoiceResolver(new StaticPalaceLawCatalog(), Mock.Of<ICatalogPalaceLawDefinitionProvider>(), Mock.Of<ICatalogEffectSetProvider>());

        var result = sut.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("accept-law");
        result.Message.Should().Contain("Loi du Silence");

        context.Run.ActivePalaceLaws.Should().ContainSingle();

        var activeLaw = context.Run.ActivePalaceLaws.Single();

        activeLaw.Key.Should().Be("law-silence-v1");
        activeLaw.Name.Should().Be("Loi du Silence");
        activeLaw.Version.Should().Be("1.0.0");
        activeLaw.Domains.Should().Contain(PalaceLawDomain.Generation);
        activeLaw.Domains.Should().Contain(PalaceLawDomain.Combat);
        activeLaw.Domains.Should().Contain(PalaceLawDomain.Rewards);
    }

    [Fact]
    public void Resolve_ShouldNotActivatePalaceLaw_WhenChoiceIsRejectLaw()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "reject-law");

        var sut = new LawEventChoiceResolver(new StaticPalaceLawCatalog(), Mock.Of<ICatalogPalaceLawDefinitionProvider>(), Mock.Of<ICatalogEffectSetProvider>());

        var result = sut.Resolve(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("reject-law");

        context.Run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldThrowDomainException_WhenChoiceIsInvalid()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "unknown-choice");

        var sut = new LawEventChoiceResolver(new StaticPalaceLawCatalog(), Mock.Of<ICatalogPalaceLawDefinitionProvider>(), Mock.Of<ICatalogEffectSetProvider>());

        var act = () => sut.Resolve(context);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Choice 'unknown-choice' is not valid for event type 'Law'.");
    }
}