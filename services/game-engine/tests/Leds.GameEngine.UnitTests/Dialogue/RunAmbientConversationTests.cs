using FluentAssertions;
using Leds.GameEngine.Domain.Dialogue;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Dialogue;

public sealed class RunAmbientConversationTests
{
    [Fact]
    public void GetOrCreateAmbientConversationState_ShouldCreateOnFirstReference()
    {
        var run = TestGameEngineFactory.CreateRun();

        var state = run.GetOrCreateAmbientConversationState("ambient.hall.majordome-sigh");

        state.AmbientConversationKey.Should().Be("ambient.hall.majordome-sigh");
        run.AmbientConversationStates.Should().ContainSingle();
    }

    [Fact]
    public void GetOrCreateAmbientConversationState_ShouldReturnTheSameInstance_OnASecondReference()
    {
        var run = TestGameEngineFactory.CreateRun();
        var first = run.GetOrCreateAmbientConversationState("ambient.hall.majordome-sigh");

        var second = run.GetOrCreateAmbientConversationState("ambient.hall.majordome-sigh");

        second.Should().BeSameAs(first);
        run.AmbientConversationStates.Should().ContainSingle();
    }

    [Fact]
    public void RehydrateAmbientConversationState_ShouldRestoreIt()
    {
        var run = TestGameEngineFactory.CreateRun();
        var state = AmbientConversationState.Create("ambient.hall.majordome-sigh");
        state.RegisterTrigger(currentStep: 3, lineCount: 2);

        run.RehydrateAmbientConversationState(state);

        run.GetOrCreateAmbientConversationState("ambient.hall.majordome-sigh").TimesTriggered.Should().Be(1);
    }
}
