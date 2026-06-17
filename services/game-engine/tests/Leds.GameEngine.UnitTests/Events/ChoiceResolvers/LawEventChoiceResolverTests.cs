using FluentAssertions;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class LawEventChoiceResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldActivatePalaceLaw_WhenChoiceIsAcceptLaw()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law:law-aegis-v1");

        var sut = new LawEventChoiceResolver(new InMemoryCatalogContentGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("accept-law:law-aegis-v1");
        result.Message.Should().Contain("Loi de l'Égide");

        context.Run.ActivePalaceLaws.Should().ContainSingle();

        var activeLaw = context.Run.ActivePalaceLaws.Single();

        activeLaw.Key.Should().Be("law-aegis-v1");
        activeLaw.Name.Should().Be("Loi de l'Égide");
        activeLaw.Version.Should().Be("1.0.0");
        activeLaw.Domains.Should().Contain(PalaceLawDomain.Combat);
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotActivatePalaceLaw_WhenChoiceIsRejectLaw()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "reject-law:law-aegis-v1");

        var sut = new LawEventChoiceResolver(new InMemoryCatalogContentGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("reject-law:law-aegis-v1");

        context.Run.ActivePalaceLaws.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldExposeRoomClimate_WhenAcceptingClimateLaw()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "accept-law:law-tempest-v1");

        var sut = new LawEventChoiceResolver(new InMemoryCatalogContentGateway());

        await sut.ResolveAsync(context);

        var climateModifier = context.Run.RunModifiers.Single(modifier =>
            modifier.Type == RunModifierType.RoomClimate);
        climateModifier.ExpiresAtRoomId.Should().Be(context.Run.CurrentRoom.Id.Value);
        climateModifier.Value.Should().Be(2);

        var dto = RunDto.FromDomain(context.Run);
        dto.CurrentRoom.ActiveClimate.Should().Be("Rain");
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowDomainException_WhenChoiceIsInvalid()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Law);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "unknown-choice");

        var sut = new LawEventChoiceResolver(new InMemoryCatalogContentGateway());

        var act = async () => await sut.ResolveAsync(context);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Choice 'unknown-choice' is not valid for event type 'Law'.");
    }
}
