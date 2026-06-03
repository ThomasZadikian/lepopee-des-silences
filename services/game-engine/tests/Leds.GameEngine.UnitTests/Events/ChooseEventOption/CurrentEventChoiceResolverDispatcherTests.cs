using FluentAssertions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChooseEventOption;

public sealed class CurrentEventChoiceResolverDispatcherTests
{
    [Fact]
    public void Resolve_ShouldUseResolverMatchingPrimaryEventType()
    {
        var context = TestCurrentEventChoiceResolutionContextFactory.Create(
            NodeEventType.Npc,
            "listen");

        var dispatcher = new CurrentEventChoiceResolverDispatcher(new ICurrentEventChoiceResolver[]
        {
            new StubCurrentEventChoiceResolver(NodeEventType.Npc, "npc-choice-resolved"),
            new StubCurrentEventChoiceResolver(NodeEventType.Law, "law-choice-resolved")
        });

        var result = dispatcher.Resolve(context);

        result.Message.Should().Be("npc-choice-resolved");
    }

    [Fact]
    public void Resolve_ShouldThrowDomainException_WhenNoResolverIsRegistered()
    {
        var context = TestCurrentEventChoiceResolutionContextFactory.Create(
            NodeEventType.Combat,
            "any-choice");

        var dispatcher = new CurrentEventChoiceResolverDispatcher(new ICurrentEventChoiceResolver[]
        {
            new StubCurrentEventChoiceResolver(NodeEventType.Npc, "npc-choice-resolved")
        });

        var act = () => dispatcher.Resolve(context);

        act.Should()
            .Throw<DomainException>()
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

        public CurrentEventChoiceResolutionResult Resolve(
            CurrentEventChoiceResolutionContext context)
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                _message);
        }
    }
}