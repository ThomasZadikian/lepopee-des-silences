using FluentAssertions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChooseEventOption;

public sealed class CurrentEventChoiceResolverDispatcherTests
{
    [Fact]
    public async Task ResolveAsync_ShouldUseResolverMatchingPrimaryEventType()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "listen");

        var dispatcher = new CurrentEventChoiceResolverDispatcher(new ICurrentEventChoiceResolver[]
        {
            new StubCurrentEventChoiceResolver(NodeEventType.Npc, "npc-choice-resolved"),
            new StubCurrentEventChoiceResolver(NodeEventType.Law, "law-choice-resolved")
        });

        var result = await dispatcher.ResolveAsync(context);

        result.Message.Should().Be("npc-choice-resolved");
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowDomainException_WhenNoResolverIsRegistered()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Combat);
        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "any-choice");

        var dispatcher = new CurrentEventChoiceResolverDispatcher(new ICurrentEventChoiceResolver[]
        {
            new StubCurrentEventChoiceResolver(NodeEventType.Npc, "npc-choice-resolved")
        });

        var act = async () => await dispatcher.ResolveAsync(context);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Current event type 'Combat' does not accept player choices.");
    }

    private sealed class StubCurrentEventChoiceResolver : ICurrentEventChoiceResolver
    {
        private readonly string _message;

        public StubCurrentEventChoiceResolver(
            NodeEventType eventType,
            string message)
        {
            EventType = eventType;
            _message = message;
        }

        public NodeEventType EventType { get; }

        public Task<CurrentEventChoiceResolutionResult> ResolveAsync(
            CurrentEventChoiceResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                _message));
        }
    }
}
