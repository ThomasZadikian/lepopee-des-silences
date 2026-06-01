using FluentAssertions;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.UnitTests.Events.ChooseEventOption;

public sealed class CurrentEventChoiceRequirementResolverTests
{
    [Fact]
    public void RequiresChoice_ShouldReturnTrue_WhenResolverExistsForNodeEventType()
    {
        // Arrange
        var node = Node.Create(
            NodeEventType.Npc,
            riskLevel: 10,
            rewardProfile: "narrative-choice");

        var sut = new CurrentEventChoiceRequirementResolver(
            new ICurrentEventChoiceResolver[]
            {
                new StubChoiceResolver(NodeEventType.Npc)
            });

        // Act
        var result = sut.RequiresChoice(node);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresChoice_ShouldReturnFalse_WhenNoResolverExistsForNodeEventType()
    {
        // Arrange
        var node = Node.Create(
            NodeEventType.Combat,
            riskLevel: 20,
            rewardProfile: "combat-common");

        var sut = new CurrentEventChoiceRequirementResolver(
            new ICurrentEventChoiceResolver[]
            {
                new StubChoiceResolver(NodeEventType.Npc)
            });

        // Act
        var result = sut.RequiresChoice(node);

        // Assert
        result.Should().BeFalse();
    }

    private sealed class StubChoiceResolver : ICurrentEventChoiceResolver
    {
        public StubChoiceResolver(NodeEventType eventType)
        {
            EventType = eventType;
        }

        public NodeEventType EventType { get; }

        public CurrentEventChoiceResolutionResult Resolve(
            CurrentEventChoiceResolutionContext context)
        {
            return CurrentEventChoiceResolutionResult.Create(
                context.ChoiceId,
                accepted: true,
                "Choice resolved.");
        }
    }
}