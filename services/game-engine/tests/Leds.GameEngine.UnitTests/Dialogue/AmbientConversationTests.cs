using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Dialogue;

namespace Leds.GameEngine.UnitTests.Dialogue;

public sealed class AmbientConversationTests
{
    [Fact]
    public void Create_ShouldRejectNoLines()
    {
        var act = () => AmbientConversation.Create(
            "ambient.hall.majordome-sigh", AmbientTriggerType.Room, "room.hall", [], cooldownSteps: 5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectNegativeCooldown()
    {
        var act = () => AmbientConversation.Create(
            "ambient.hall.majordome-sigh", AmbientTriggerType.Room, "room.hall", ["..."], cooldownSteps: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CanTrigger_ShouldBeTrue_BeforeAnyTrigger()
    {
        var state = AmbientConversationState.Create("ambient.hall.majordome-sigh");

        state.CanTrigger(currentStep: 0, cooldownSteps: 5).Should().BeTrue();
    }

    [Fact]
    public void CanTrigger_ShouldBeFalse_WithinCooldown_AndTrueAfter()
    {
        var state = AmbientConversationState.Create("ambient.hall.majordome-sigh");
        state.RegisterTrigger(currentStep: 10, lineCount: 3);

        state.CanTrigger(currentStep: 12, cooldownSteps: 5).Should().BeFalse();
        state.CanTrigger(currentStep: 15, cooldownSteps: 5).Should().BeTrue();
    }

    [Fact]
    public void RegisterTrigger_ShouldRotateVariantsRoundRobin()
    {
        var state = AmbientConversationState.Create("ambient.hall.majordome-sigh");

        var first = state.RegisterTrigger(currentStep: 0, lineCount: 3);
        var second = state.RegisterTrigger(currentStep: 10, lineCount: 3);
        var third = state.RegisterTrigger(currentStep: 20, lineCount: 3);
        var fourth = state.RegisterTrigger(currentStep: 30, lineCount: 3);

        new[] { first, second, third, fourth }.Should().Equal(0, 1, 2, 0);
        state.TimesTriggered.Should().Be(4);
    }

    [Fact]
    public void Rehydrate_ShouldPreserveState()
    {
        var state = AmbientConversationState.Create("ambient.hall.majordome-sigh");
        state.RegisterTrigger(currentStep: 7, lineCount: 2);

        var rehydrated = AmbientConversationState.Rehydrate(
            state.AmbientConversationKey, state.LastTriggeredStep, state.TimesTriggered);

        rehydrated.LastTriggeredStep.Should().Be(7);
        rehydrated.TimesTriggered.Should().Be(1);
    }
}
