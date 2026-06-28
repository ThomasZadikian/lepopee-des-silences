using FluentAssertions;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class NpcEventChoiceResolverTests
{
    [Fact]
    public void EventType_ShouldReturnNpc()
    {
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway());

        sut.EventType.Should().Be(NodeEventType.Npc);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnFade_WhenNoActiveNpcKey()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);

        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "greet");

        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.Message.Should().Contain("efface");
        result.EncounterCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnFade_WhenNpcHasNoDialogueGraph()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);
        runWithNode.Run.BeginOrResumeNpcEncounter("npc-neutral-traveler");

        var context = new CurrentEventChoiceResolutionContext(
            runWithNode.Run,
            runWithNode.Run.CurrentRoom,
            runWithNode.TargetNode,
            "greet");

        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.Message.Should().Contain("efface");
        result.EncounterCompleted.Should().BeTrue();
    }
}
