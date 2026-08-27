using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Events;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events;

public sealed class NpcDialogueViewFactoryCoverageTests
{
    [Fact]
    public void Build_ShouldReturnSilentView_WhenDialogueGraphIsMissing()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Begin("npc.test", null);
        var npc = CreateNpc(graph: null);

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result.Should().NotBeNull();
        result!.EncounterActive.Should().BeFalse();
        result.Choices.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnSilentView_WhenCurrentNodeDoesNotExist()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Begin("npc.test", "missing");
        var npc = CreateNpc(CreateGraph(CreateNode([])));

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result.Should().NotBeNull();
        result!.EncounterActive.Should().BeFalse();
        result.NodeKey.Should().Be("missing");
    }

    [Theory]
    [InlineData(WoundState.Latent, "base")]
    [InlineData(WoundState.Tendu, "tense")]
    [InlineData(WoundState.Rompu, "ruptured")]
    public void Build_ShouldSelectLinesFromAggregateWoundState(WoundState state, string expected)
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Rehydrate(
            "npc.test", 0,
            new Dictionary<string, WoundState> { ["wound"] = state },
            [], 1, "entry");
        var node = CreateNode([], ["base"], ["tense"], ["ruptured"]);
        var npc = CreateNpc(CreateGraph(node));

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result!.Lines.Should().ContainSingle(expected);
        result.EncounterActive.Should().BeTrue();
    }

    [Fact]
    public void Build_ShouldFilterFlagRequirementsAcrossPositiveAndNegativeCases()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Begin("npc.test", "entry");
        relationship.SetFlag("present");

        var choices = new[]
        {
            Choice("present-ok", Req("FlagPresent", flagKey: "present")),
            Choice("present-null", Req("FlagPresent")),
            Choice("present-missing", Req("FlagPresent", flagKey: "missing")),
            Choice("absent-ok", Req("FlagAbsent", flagKey: "missing")),
            Choice("absent-null", Req("FlagAbsent")),
            Choice("absent-blocked", Req("FlagAbsent", flagKey: "present"))
        };
        var npc = CreateNpc(CreateGraph(CreateNode(choices)));

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result!.Choices.Select(x => x.Id).Should().BeEquivalentTo(
            ["present-ok", "absent-ok", "absent-null"]);
    }

    [Fact]
    public void Build_ShouldFilterWoundAndRelationshipScoreRequirements()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Rehydrate(
            "npc.test", 12,
            new Dictionary<string, WoundState> { ["scar"] = WoundState.Tendu },
            [], 1, "entry");

        var choices = new[]
        {
            Choice("wound-ok", Req("WoundStateAtLeast", woundKey: "scar", requiredWoundState: "Tendu")),
            Choice("wound-too-high", Req("WoundStateAtLeast", woundKey: "scar", requiredWoundState: "Rompu")),
            Choice("wound-null", Req("WoundStateAtLeast", requiredWoundState: "Tendu")),
            Choice("wound-invalid", Req("WoundStateAtLeast", woundKey: "scar", requiredWoundState: "not-a-state")),
            Choice("score-ok", Req("RelationshipScoreAtLeast", requiredScore: 10)),
            Choice("score-low", Req("RelationshipScoreAtLeast", requiredScore: 13)),
            Choice("score-null", Req("RelationshipScoreAtLeast"))
        };
        var npc = CreateNpc(CreateGraph(CreateNode(choices)));

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result!.Choices.Select(x => x.Id).Should().BeEquivalentTo(["wound-ok", "score-ok"]);
    }

    [Fact]
    public void Build_ShouldEvaluateBalancedAndCompanionRequirements()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();
        var relationship = NpcRelationship.Begin("npc.test", "entry");

        var protagonist = run.PlayerSnapshot!.Characters.Single();
        var snapshot = Leds.GameEngine.Domain.Runs.RunPlayerSnapshot.Create(
            run.PlayerId,
            "Joueur",
            [protagonist],
            DateTimeOffset.UtcNow);
        run.AttachPlayerSnapshot(snapshot);

        var choices = new[]
        {
            Choice("balanced", Req("PlayerStatsBalanced")),
            Choice("unbalanced", Req("PlayerStatsUnbalanced")),
            Choice("has-self", Req("PlayerHasCompanion", flagKey: protagonist.DefinitionKey)),
            Choice("has-null", Req("PlayerHasCompanion")),
            Choice("has-missing", Req("PlayerHasCompanion", flagKey: "character.missing")),
            Choice("lacks-missing", Req("PlayerLacksCompanion", flagKey: "character.missing")),
            Choice("lacks-null", Req("PlayerLacksCompanion")),
            Choice("lacks-self", Req("PlayerLacksCompanion", flagKey: protagonist.DefinitionKey))
        };
        var npc = CreateNpc(CreateGraph(CreateNode(choices)));

        var result = NpcDialogueViewFactory.Build(npc, relationship, run);

        result!.Choices.Select(x => x.Id).Should().Contain(["has-self", "lacks-missing", "lacks-null"]);
        result.Choices.Select(x => x.Id).Should().NotContain(["has-null", "has-missing", "lacks-self"]);
        result.Choices.Should().ContainSingle(c => c.Id is "balanced" or "unbalanced");
    }

    private static CatalogNpcDefinition CreateNpc(CatalogNpcDialogueGraph? graph) =>
        new(
            "npc.test",
            "Test NPC",
            "description",
            [],
            ["Threshold"],
            [PalaceRoomState.Calm],
            [],
            DialogueGraph: graph);

    private static CatalogNpcDialogueGraph CreateGraph(CatalogNpcDialogueNode node) =>
        new("graph.test", "1.0.0", "entry",
            new Dictionary<string, CatalogNpcDialogueNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["entry"] = node
            });

    private static CatalogNpcDialogueNode CreateNode(
        IReadOnlyCollection<CatalogNpcDialogueChoice> choices,
        IReadOnlyCollection<string>? lines = null,
        IReadOnlyCollection<string>? tenseLines = null,
        IReadOnlyCollection<string>? rupturedLines = null) =>
        new("entry", "Test NPC", lines ?? ["base"], choices, tenseLines, rupturedLines);

    private static CatalogNpcDialogueChoice Choice(string key, params CatalogDialogueRequirement[] requirements) =>
        new(key, key, requirements, [], null);

    private static CatalogDialogueRequirement Req(
        string kind,
        string? flagKey = null,
        string? woundKey = null,
        string? requiredWoundState = null,
        int? requiredScore = null) =>
        new(kind, flagKey, woundKey, requiredWoundState, requiredScore);
}
