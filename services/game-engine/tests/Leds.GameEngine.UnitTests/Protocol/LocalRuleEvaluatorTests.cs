using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Protocol;

namespace Leds.GameEngine.UnitTests.Protocol;

public sealed class LocalRuleEvaluatorTests
{
    /// <summary>Carpet zone at (5,5)-(6,5); Look at severity 1, NpcRelocate (majordome) at 2,
    /// Combat only at 3 — mirrors the Contrat/SFD Hall rule that combat is never the first
    /// consequence of a misstep.</summary>
    private static LocalRule CreateTapisRule() => LocalRule.Create(
        "rule.hall.tapis", "Tapis rouge", LocalRuleConditionType.ZoneEntry,
        infoMessage: "Le tapis rouge ne doit pas etre foule.",
        warningMessage: "Vous avez ete averti : ne marchez plus sur le tapis.",
        consequences:
        [
            LocalRuleConsequence.Create(1, LocalRuleConsequenceType.Look),
            LocalRuleConsequence.Create(2, LocalRuleConsequenceType.NpcRelocate, "npc.majordome"),
            LocalRuleConsequence.Create(3, LocalRuleConsequenceType.Combat),
        ],
        conditionCells: [(5, 5), (6, 5)]);

    private static LocalRule CreateMajordomeInteractionRule() => LocalRule.Create(
        "rule.hall.majordome-contact", "Contact du majordome", LocalRuleConditionType.NpcInteraction,
        infoMessage: "Le majordome vous observe.",
        warningMessage: "Le majordome fronce les sourcils.",
        consequences: [LocalRuleConsequence.Create(1, LocalRuleConsequenceType.Warning)],
        conditionTargetKey: "npc.majordome");

    [Fact]
    public void Evaluate_ShouldReturnNotApplicable_WhenPartyIsOutsideTheZone()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);

        var result = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(0, 0));

        result.Outcome.Should().Be(LocalRuleEvaluationOutcome.NotApplicable);
        state.HasBeenInformed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ShouldReturnNotApplicable_WhenInteractionTargetDoesNotMatch()
    {
        var rule = CreateMajordomeInteractionRule();
        var state = LocalRuleState.Create(rule.Key);

        var result = LocalRuleEvaluator.Evaluate(
            rule, state, LocalRuleTriggerContext.ForInteraction("npc.someone-else"));

        result.Outcome.Should().Be(LocalRuleEvaluationOutcome.NotApplicable);
    }

    [Fact]
    public void Evaluate_FirstContact_ShouldInform_WithoutAccruingSeverity()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);

        var result = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Informed);
        result.Message.Should().Be(rule.InfoMessage);
        result.NewConsequences.Should().BeEmpty();
        state.HasBeenInformed.Should().BeTrue();
        state.CumulativeSeverity.Should().Be(0);
    }

    [Fact]
    public void Evaluate_SecondContact_ShouldBeAFirstTransgression_UnlockingOnlyThresholdOne()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);
        LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        var result = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        result.Outcome.Should().Be(LocalRuleEvaluationOutcome.Transgression);
        result.Message.Should().Be(rule.WarningMessage);
        result.NewConsequences.Should().ContainSingle(c => c.Type == LocalRuleConsequenceType.Look);
        // No automatic combat on a single misstep (Contrat/SFD Hall §V).
        result.NewConsequences.Should().NotContain(c => c.Type == LocalRuleConsequenceType.Combat);
        state.CumulativeSeverity.Should().Be(1);
    }

    [Fact]
    public void Evaluate_RepeatedTransgressions_ShouldEscalateWithoutRepeatingConsequences()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);
        LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5)); // informed

        var first = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));
        var second = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(6, 5));
        var third = LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        first.Message.Should().Be(rule.WarningMessage);
        first.NewConsequences.Select(c => c.Type).Should().Equal(LocalRuleConsequenceType.Look);

        second.Message.Should().BeNull(); // warning already shown once
        second.NewConsequences.Select(c => c.Type).Should().Equal(LocalRuleConsequenceType.NpcRelocate);

        third.NewConsequences.Select(c => c.Type).Should().Equal(LocalRuleConsequenceType.Combat);

        state.CumulativeSeverity.Should().Be(3);
        state.TriggeredThresholds.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenStateBelongsToADifferentRule()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create("rule.some-other-rule");

        var act = () => LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RegisterTransgression_ShouldThrow_WhenSeverityIncrementIsBelowOne()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);
        state.MarkInformed();

        var act = () => state.RegisterTransgression(rule, severityIncrement: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rehydrate_ShouldPreserveAllFields()
    {
        var rule = CreateTapisRule();
        var state = LocalRuleState.Create(rule.Key);
        LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));
        LocalRuleEvaluator.Evaluate(rule, state, LocalRuleTriggerContext.ForPosition(5, 5));

        var rehydrated = LocalRuleState.Rehydrate(
            state.LocalRuleKey, state.CumulativeSeverity, state.HasBeenInformed, state.TriggeredThresholds);

        rehydrated.LocalRuleKey.Should().Be(state.LocalRuleKey);
        rehydrated.CumulativeSeverity.Should().Be(state.CumulativeSeverity);
        rehydrated.HasBeenInformed.Should().Be(state.HasBeenInformed);
        rehydrated.TriggeredThresholds.Should().BeEquivalentTo(state.TriggeredThresholds);
    }
}
