using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Events.Npcs;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.UnitTests.Events.Npcs;

public sealed class NpcEncounterSelectorTests
{
    private static readonly IReadOnlyCollection<CatalogNpcDefinition> SeedNpcs =
    [
        new CatalogNpcDefinition(
            Key: "npc-neutral-traveler",
            DisplayName: "Voyageur Neutre",
            Description: "Un vagabond sans attache.",
            Tags: ["generic", "neutral"],
            CompatibleRoomTypes: [],
            CompatiblePalaceRoomStates: [],
            CompatibleRoomClimates: []),
        new CatalogNpcDefinition(
            Key: "npc-silent-witness",
            DisplayName: "Témoin Silencieux",
            Description: "Une silhouette figée.",
            Tags: ["silence", "witness"],
            CompatibleRoomTypes: [],
            CompatiblePalaceRoomStates: [PalaceRoomState.Silent],
            CompatibleRoomClimates: []),
        new CatalogNpcDefinition(
            Key: "npc-desert-exile",
            DisplayName: "Exilé du Désert",
            Description: "Un habitant des sables brûlants.",
            Tags: ["desert", "exile"],
            CompatibleRoomTypes: [],
            CompatiblePalaceRoomStates: [],
            CompatibleRoomClimates: ["Heatwave"]),
        new CatalogNpcDefinition(
            Key: "npc-rage-beggar",
            DisplayName: "Mendiant de Rage",
            Description: "Un être consumé par la colère.",
            Tags: ["rage", "aggressive"],
            CompatibleRoomTypes: [],
            CompatiblePalaceRoomStates: [PalaceRoomState.Enraged],
            CompatibleRoomClimates: []),
        new CatalogNpcDefinition(
            Key: "npc-rain-memory-keeper",
            DisplayName: "Gardien des Mémoires de Pluie",
            Description: "Un archiviste des souvenirs.",
            Tags: ["rain", "memory"],
            CompatibleRoomTypes: [],
            CompatiblePalaceRoomStates: [],
            CompatibleRoomClimates: ["Rain"])
    ];

    private static NpcEligibilityContext CreateContext(
        string seed = "test-seed",
        PalaceRoomState palaceState = PalaceRoomState.Neutral,
        string? climate = null,
        RoomType roomType = RoomType.Threshold,
        int nodeDepth = 1) =>
        new(
            RunId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            Seed: seed,
            PalaceRoomState: palaceState,
            RoomClimate: climate,
            RoomType: roomType,
            NodeDepth: nodeDepth);

    [Fact]
    public void SelectEligibleNpc_ShouldReturnNull_WhenNoNpcs()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(CreateContext(), []);
        result.Should().BeNull();
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnNeutralTraveler_WhenNoConstraintsMatch()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(CreateContext(), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-neutral-traveler");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnSilentWitness_WhenStateIsSilent()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(
            CreateContext(palaceState: PalaceRoomState.Silent), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-silent-witness");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnDesertExile_WhenClimateIsHeatwave()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(
            CreateContext(climate: "Heatwave"), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-desert-exile");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnRageBeggar_WhenStateIsEnraged()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(
            CreateContext(palaceState: PalaceRoomState.Enraged), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-rage-beggar");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnRainMemoryKeeper_WhenClimateIsRain()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(
            CreateContext(climate: "Rain"), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-rain-memory-keeper");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldFallbackToNeutral_WhenNoSpecificMatch()
    {
        var selector = new NpcEncounterSelector();
        var result = selector.SelectEligibleNpc(
            CreateContext(palaceState: PalaceRoomState.Painful), SeedNpcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("npc-neutral-traveler");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldBeDeterministic_ForSameContext()
    {
        var selector = new NpcEncounterSelector();
        var context = CreateContext(seed: "fixed-seed");

        var result1 = selector.SelectEligibleNpc(context, SeedNpcs);
        var result2 = selector.SelectEligibleNpc(context, SeedNpcs);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Key.Should().Be(result2!.Key);
    }

    [Fact]
    public void SelectEligibleNpc_ShouldRespectMinDepth()
    {
        var npcs = new[]
        {
            new CatalogNpcDefinition(
                Key: "deep-npc",
                DisplayName: "Deep",
                Description: "Only appears deep.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [],
                MinDepth: 5,
                MaxDepth: int.MaxValue),
            new CatalogNpcDefinition(
                Key: "shallow-npc",
                DisplayName: "Shallow",
                Description: "Appears at any depth.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [],
                MinDepth: 0,
                MaxDepth: 4)
        };

        var selector = new NpcEncounterSelector();

        var shallowResult = selector.SelectEligibleNpc(
            CreateContext(nodeDepth: 1), npcs);
        shallowResult.Should().NotBeNull();
        shallowResult!.Key.Should().Be("shallow-npc");

        var deepResult = selector.SelectEligibleNpc(
            CreateContext(nodeDepth: 10), npcs);
        deepResult.Should().NotBeNull();
        deepResult!.Key.Should().Be("deep-npc");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldRespectMaxDepth()
    {
        var npcs = new[]
        {
            new CatalogNpcDefinition(
                Key: "early-npc",
                DisplayName: "Early",
                Description: "Only appears early.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [],
                MinDepth: 0,
                MaxDepth: 3),
            new CatalogNpcDefinition(
                Key: "late-npc",
                DisplayName: "Late",
                Description: "Appears later.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [],
                MinDepth: 5,
                MaxDepth: int.MaxValue)
        };

        var selector = new NpcEncounterSelector();

        var earlyResult = selector.SelectEligibleNpc(
            CreateContext(nodeDepth: 1), npcs);
        earlyResult.Should().NotBeNull();
        earlyResult!.Key.Should().Be("early-npc");

        var lateResult = selector.SelectEligibleNpc(
            CreateContext(nodeDepth: 5), npcs);
        lateResult.Should().NotBeNull();
        lateResult!.Key.Should().Be("late-npc");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldRespecRoomTypeConstraint()
    {
        var npcs = new[]
        {
            new CatalogNpcDefinition(
                Key: "threshold-npc",
                DisplayName: "Threshold NPC",
                Description: "Only in Threshold.",
                Tags: [],
                CompatibleRoomTypes: ["Threshold"],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: []),
            new CatalogNpcDefinition(
                Key: "forest-npc",
                DisplayName: "Forest NPC",
                Description: "Only in Forest.",
                Tags: [],
                CompatibleRoomTypes: ["Forest"],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [])
        };

        var selector = new NpcEncounterSelector();

        var thresholdResult = selector.SelectEligibleNpc(
            CreateContext(roomType: RoomType.Threshold), npcs);
        thresholdResult.Should().NotBeNull();
        thresholdResult!.Key.Should().Be("threshold-npc");

        var forestResult = selector.SelectEligibleNpc(
            CreateContext(roomType: RoomType.Forest), npcs);
        forestResult.Should().NotBeNull();
        forestResult!.Key.Should().Be("forest-npc");
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnNull_WhenAllEligibleFilteredByConstraints()
    {
        var npcs = new[]
        {
            new CatalogNpcDefinition(
                Key: "only-threshold",
                DisplayName: "Only Threshold",
                Description: "Only in Threshold.",
                Tags: [],
                CompatibleRoomTypes: ["Threshold"],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: [])
        };

        var selector = new NpcEncounterSelector();

        var result = selector.SelectEligibleNpc(
            CreateContext(roomType: RoomType.Forest), npcs);
        result.Should().BeNull();
    }

    [Fact]
    public void SelectEligibleNpc_ShouldReturnSpecificMatch_OverFallback_WhenBothFit()
    {
        var npcs = new[]
        {
            new CatalogNpcDefinition(
                Key: "neutral-npc",
                DisplayName: "Neutral",
                Description: "Works everywhere.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [],
                CompatibleRoomClimates: []),
            new CatalogNpcDefinition(
                Key: "silent-specific",
                DisplayName: "Silent Only",
                Description: "Only in Silent.",
                Tags: [],
                CompatibleRoomTypes: [],
                CompatiblePalaceRoomStates: [PalaceRoomState.Silent],
                CompatibleRoomClimates: [])
        };

        var selector = new NpcEncounterSelector();

        var result = selector.SelectEligibleNpc(
            CreateContext(palaceState: PalaceRoomState.Silent), npcs);
        result.Should().NotBeNull();
        result!.Key.Should().Be("silent-specific");
    }
}
