using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class NpcEventChoiceResolverRequirementCoverageTests
{
    private static readonly MethodInfo RequirementsMetMethod =
        typeof(NpcEventChoiceResolver).GetMethod(
            "RequirementsMet",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("RequirementsMet was not found.");

    private static readonly MethodInfo IsPlayerStatsBalancedMethod =
        typeof(NpcEventChoiceResolver).GetMethod(
            "IsPlayerStatsBalanced",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("IsPlayerStatsBalanced was not found.");

    private static readonly MethodInfo HasCompanionMethod =
        typeof(NpcEventChoiceResolver).GetMethod(
            "HasCompanion",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("HasCompanion was not found.");

    [Fact]
    public void RequirementsMet_ShouldAcceptEmptyAndUnknownRequirements()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeRequirements([], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("future-kind")], relationship, run).Should().BeTrue();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverFlagPresenceAndAbsence()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeRequirements([Requirement("FlagPresent", flagKey: null)], relationship, run).Should().BeFalse();
        InvokeRequirements([Requirement("FlagPresent", flagKey: "seen")], relationship, run).Should().BeFalse();
        relationship.SetFlag("seen");
        InvokeRequirements([Requirement("FlagPresent", flagKey: "seen")], relationship, run).Should().BeTrue();

        InvokeRequirements([Requirement("FlagAbsent", flagKey: null)], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("FlagAbsent", flagKey: "other")], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("FlagAbsent", flagKey: "seen")], relationship, run).Should().BeFalse();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverWoundThresholdValidation()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: null, state: "Tendu")], relationship, run)
            .Should().BeFalse();
        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: "w", state: "not-a-state")], relationship, run)
            .Should().BeFalse();
        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: "w", state: "Tendu")], relationship, run)
            .Should().BeFalse();

        relationship.SetWoundState("w", WoundState.Tendu, canRevert: false);
        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: "w", state: "Tendu")], relationship, run)
            .Should().BeTrue();
        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: "w", state: "Latent")], relationship, run)
            .Should().BeTrue();
        InvokeRequirements([Requirement("WoundStateAtLeast", woundKey: "w", state: "Rompu")], relationship, run)
            .Should().BeFalse();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverRelationshipScoreThresholds()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeRequirements([Requirement("RelationshipScoreAtLeast")], relationship, run).Should().BeFalse();
        InvokeRequirements([Requirement("RelationshipScoreAtLeast", score: 1)], relationship, run).Should().BeFalse();
        InvokeRequirements([Requirement("RelationshipScoreAtLeast", score: 0)], relationship, run).Should().BeTrue();

        relationship.AdjustScore(5);
        InvokeRequirements([Requirement("RelationshipScoreAtLeast", score: 5)], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("RelationshipScoreAtLeast", score: 6)], relationship, run).Should().BeFalse();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverContainerRequirement()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeRequirements([Requirement("PlayerHasContainerItem")], relationship, run).Should().BeFalse();

        run.AddRunItem(RunItem.Create(
            "item.test.container", "Fiole", "Container",
            RunItemType.Passive, RunItemRarity.Common, 1,
            RunItemEffectType.None, 0,
            isContainer: true, containerCapacity: 1));

        InvokeRequirements([Requirement("PlayerHasContainerItem")], relationship, run).Should().BeTrue();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverBalancedAndUnbalancedStats()
    {
        var balanced = CreateRunWithStats(10, 10, 10, 10);
        var unbalanced = CreateRunWithStats(20, 1, 10, 1);
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeBalanced(balanced).Should().BeTrue();
        InvokeBalanced(unbalanced).Should().BeFalse();

        InvokeRequirements([Requirement("PlayerStatsBalanced")], relationship, balanced).Should().BeTrue();
        InvokeRequirements([Requirement("PlayerStatsBalanced")], relationship, unbalanced).Should().BeFalse();
        InvokeRequirements([Requirement("PlayerStatsUnbalanced")], relationship, balanced).Should().BeFalse();
        InvokeRequirements([Requirement("PlayerStatsUnbalanced")], relationship, unbalanced).Should().BeTrue();
    }

    [Fact]
    public void RequirementsMet_ShouldCoverCompanionPresenceAndAbsence()
    {
        var run = CreateRunWithStats(10, 10, 10, 10, "character.player.self", "character.friend");
        var relationship = NpcRelationship.Begin("npc.test", null);

        InvokeHasCompanion(run, "character.friend").Should().BeTrue();
        InvokeHasCompanion(run, "CHARACTER.FRIEND").Should().BeTrue();
        InvokeHasCompanion(run, "character.missing").Should().BeFalse();

        InvokeRequirements([Requirement("PlayerHasCompanion", flagKey: null)], relationship, run).Should().BeFalse();
        InvokeRequirements([Requirement("PlayerHasCompanion", flagKey: "character.friend")], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("PlayerHasCompanion", flagKey: "character.missing")], relationship, run).Should().BeFalse();

        InvokeRequirements([Requirement("PlayerLacksCompanion", flagKey: null)], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("PlayerLacksCompanion", flagKey: "character.missing")], relationship, run).Should().BeTrue();
        InvokeRequirements([Requirement("PlayerLacksCompanion", flagKey: "character.friend")], relationship, run).Should().BeFalse();
    }

    [Fact]
    public void RequirementsMet_ShouldStopAtFirstFailedRequirement()
    {
        var run = CreateRunWithStats(10, 10, 10, 10);
        var relationship = NpcRelationship.Begin("npc.test", null);
        relationship.SetFlag("present");

        InvokeRequirements(
        [
            Requirement("FlagPresent", flagKey: "present"),
            Requirement("FlagPresent", flagKey: "missing"),
            Requirement("future-kind")
        ], relationship, run).Should().BeFalse();
    }

    private static CatalogDialogueRequirement Requirement(
        string kind,
        string? flagKey = null,
        string? woundKey = null,
        string? state = null,
        int? score = null) =>
        new(kind, flagKey, woundKey, state, score);

    private static bool InvokeRequirements(
        IReadOnlyCollection<CatalogDialogueRequirement> requirements,
        NpcRelationship relationship,
        Run run) =>
        (bool)RequirementsMetMethod.Invoke(null, [requirements, relationship, run])!;

    private static bool InvokeBalanced(Run run) =>
        (bool)IsPlayerStatsBalancedMethod.Invoke(null, [run])!;

    private static bool InvokeHasCompanion(Run run, string definitionKey) =>
        (bool)HasCompanionMethod.Invoke(null, [run, definitionKey])!;

    private static Run CreateRunWithStats(
        int attack,
        int defense,
        int speed,
        int focus,
        params string[] characterKeys)
    {
        var run = TestGameEngineFactory.CreateRun();
        if (characterKeys.Length == 0)
            characterKeys = ["character.player.self"];

        var characters = characterKeys.Select((key, index) =>
        {
            var stats = RunCharacterStatSnapshot.Create(
                maxVitality: 100,
                attackPower: attack,
                defense: defense,
                startingGuard: 0,
                speed: speed,
                initiative: 10,
                focus: focus,
                mana: 0,
                charge: 0);
            return RunCharacterSnapshot.Create(
                Guid.NewGuid(), key, $"Character {index}", stats, [], "Neutral");
        }).ToArray();

        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            run.PlayerId,
            "Player",
            characters,
            DateTimeOffset.UtcNow));
        return run;
    }
}
