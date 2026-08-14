using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Protocol;

namespace Leds.GameEngine.UnitTests.Protocol;

public sealed class LocalRuleTests
{
    private static LocalRuleConsequence LookConsequence(int threshold = 1) =>
        LocalRuleConsequence.Create(threshold, LocalRuleConsequenceType.Look);

    [Fact]
    public void Create_ShouldRejectEmptyKey()
    {
        var act = () => LocalRule.Create(
            " ", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [LookConsequence()], conditionCells: [(1, 1)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ZoneEntry_ShouldRejectMissingConditionCells()
    {
        var act = () => LocalRule.Create(
            "rule.tapis", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [LookConsequence()]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ZoneEntry_ShouldRejectAConditionTargetKey()
    {
        var act = () => LocalRule.Create(
            "rule.tapis", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [LookConsequence()], conditionCells: [(1, 1)], conditionTargetKey: "x");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NpcInteraction_ShouldRejectMissingConditionTargetKey()
    {
        var act = () => LocalRule.Create(
            "rule.majordome", "Majordome", LocalRuleConditionType.NpcInteraction, "info", "warn",
            [LookConsequence()]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NpcInteraction_ShouldRejectConditionCells()
    {
        var act = () => LocalRule.Create(
            "rule.majordome", "Majordome", LocalRuleConditionType.NpcInteraction, "info", "warn",
            [LookConsequence()], conditionCells: [(1, 1)], conditionTargetKey: "npc.majordome");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectEmptyConsequences()
    {
        var act = () => LocalRule.Create(
            "rule.tapis", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [], conditionCells: [(1, 1)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldRejectDuplicateSeverityThresholds()
    {
        var act = () => LocalRule.Create(
            "rule.tapis", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [LookConsequence(1), LookConsequence(1)], conditionCells: [(1, 1)]);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldOrderConsequencesBySeverityThreshold()
    {
        var rule = LocalRule.Create(
            "rule.tapis", "Tapis", LocalRuleConditionType.ZoneEntry, "info", "warn",
            [LookConsequence(3), LookConsequence(1), LookConsequence(2)],
            conditionCells: [(1, 1)]);

        rule.Consequences.Select(c => c.SeverityThreshold).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ConsequenceCreate_ShouldRejectSeverityThresholdBelowOne()
    {
        var act = () => LocalRuleConsequence.Create(0, LocalRuleConsequenceType.Look);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(LocalRuleConsequenceType.NpcRelocate)]
    [InlineData(LocalRuleConsequenceType.AttitudeChange)]
    [InlineData(LocalRuleConsequenceType.IncreasedSurveillance)]
    public void ConsequenceCreate_ShouldRequireTargetNpc_ForNpcFacingTypes(LocalRuleConsequenceType type)
    {
        var act = () => LocalRuleConsequence.Create(1, type);

        act.Should().Throw<DomainException>();
    }
}
