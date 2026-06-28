using FluentAssertions;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class MerchantEventChoiceResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldReturnAccepted_WhenChoiceIsTrade()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Merchant);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "trade");

        var sut = new MerchantEventChoiceResolver();

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("trade");
        result.Message.Should().Contain("échange");
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnAccepted_WhenChoiceIsRefuse()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Merchant);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "refuse");

        var sut = new MerchantEventChoiceResolver();

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.ChoiceId.Should().Be("refuse");
        result.Message.Should().Contain("refuse");
        result.NarrativeFragments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowDomainException_WhenChoiceIsInvalid()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Merchant);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "unknown-choice");

        var sut = new MerchantEventChoiceResolver();

        var act = async () => await sut.ResolveAsync(context);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*not valid for event type*");
    }

    [Fact]
    public void EventType_ShouldReturnMerchant()
    {
        var sut = new MerchantEventChoiceResolver();

        sut.EventType.Should().Be(NodeEventType.Merchant);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNarrativeFragmentWithElise()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Merchant);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "trade");

        var sut = new MerchantEventChoiceResolver();

        var result = await sut.ResolveAsync(context);

        result.NarrativeFragments.Should().ContainSingle();
        result.NarrativeFragments.Single().Speaker.Should().Be("Elise");
    }
}
