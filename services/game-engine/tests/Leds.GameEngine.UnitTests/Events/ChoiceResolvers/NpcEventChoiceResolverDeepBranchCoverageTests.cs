using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class NpcEventChoiceResolverDeepBranchCoverageTests
{
    private static readonly MethodInfo IsAvailableMethod = PrivateStatic("IsAvailable");
    private static readonly MethodInfo EvaluateTransgressionsMethod = PrivateStatic("EvaluateTransgressions");
    private static readonly MethodInfo RefreshWoundsMethod = PrivateStatic("RefreshWounds");
    private static readonly MethodInfo ApplyRewardCurseEffectMethod =
        typeof(NpcEventChoiceResolver).GetMethod("ApplyRewardCurseEffectAsync", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("ApplyRewardCurseEffectAsync was not found.");

    [Fact]
    public void IsAvailable_ShouldCoverEveryAvailabilityGateAndShortCircuit()
    {
        var run = TestGameEngineFactory.CreateRun();

        Available(Entry(null!), run).Should().BeTrue();
        Available(Entry([]), run).Should().BeTrue();
        Available(Entry([Gate("future", 999)]), run).Should().BeTrue();

        Available(Entry([Gate("MinVitalityRatioPercent", 0)]), run).Should().BeTrue();
        Available(Entry([Gate("MinVitalityRatioPercent", 101)]), run).Should().BeFalse();
        Available(Entry([Gate("MaxVitalityRatioPercent", 100)]), run).Should().BeTrue();
        Available(Entry([Gate("MaxVitalityRatioPercent", -1)]), run).Should().BeFalse();

        Available(Entry([Gate("MinActiveLawCount", 0)]), run).Should().BeTrue();
        Available(Entry([Gate("MinActiveLawCount", 1)]), run).Should().BeFalse();

        Available(Entry([Gate("MinNodeDepth", 0)]), run).Should().BeTrue();
        Available(Entry([Gate("MinNodeDepth", int.MaxValue)]), run).Should().BeFalse();

        Available(Entry([
            Gate("future", 0),
            Gate("MinVitalityRatioPercent", 101),
            Gate("MaxVitalityRatioPercent", 100)
        ]), run).Should().BeFalse();

        typeof(Run).GetProperty(nameof(Run.MaxHp))!.SetValue(run, 0);
        Available(Entry([Gate("MaxVitalityRatioPercent", 0)]), run).Should().BeTrue();
    }

    [Fact]
    public void EvaluateTransgressions_ShouldHandleNullMissingTriggeredAndAlreadyArmedCases()
    {
        var run = TestGameEngineFactory.CreateRun();
        var relationship = NpcRelationship.Begin("npc.test", null);

        Invoke(EvaluateTransgressionsMethod, Npc(wounds: null), run, relationship);
        relationship.RelationshipScore.Should().Be(0);

        var wound = new CatalogNpcWound(
            "wound.test", "Rupture", "Irreversible", -1, -5,
            [new CatalogNpcTransgression("wound.test", "flag.break", -3)], null);
        var npc = Npc([wound]);

        Invoke(EvaluateTransgressionsMethod, npc, run, relationship);
        relationship.RelationshipScore.Should().Be(0);

        relationship.SetFlag("flag.break");
        Invoke(EvaluateTransgressionsMethod, npc, run, relationship);
        relationship.RelationshipScore.Should().Be(-3);
        relationship.GetWoundState("wound.test").Should().Be(WoundState.Rompu);
        relationship.HasFlag("__armed:wound.test:flag.break").Should().BeTrue();

        Invoke(EvaluateTransgressionsMethod, npc, run, relationship);
        relationship.RelationshipScore.Should().Be(-3,
            "the armed marker must prevent applying the same transgression twice");
    }

    [Fact]
    public void RefreshWounds_ShouldCoverNullReversibleAndIrreversibleWounds()
    {
        var run = TestGameEngineFactory.CreateRun();
        var relationship = NpcRelationship.Begin("npc.test", null);

        Invoke(RefreshWoundsMethod, Npc(wounds: null), run, relationship);

        var reversible = new CatalogNpcWound(
            "wound.reversible", "Rupture", "SoothableByScore", 1, -3, [], null);
        var irreversible = new CatalogNpcWound(
            "wound.irreversible", "Rupture", "Irreversible", 1, -3, [], null);
        var npc = Npc([reversible, irreversible]);

        relationship.AdjustScore(-5);
        Invoke(RefreshWoundsMethod, npc, run, relationship);
        relationship.GetWoundState("wound.reversible").Should().Be(WoundState.Rompu);
        relationship.GetWoundState("wound.irreversible").Should().Be(WoundState.Rompu);

        relationship.AdjustScore(10);
        Invoke(RefreshWoundsMethod, npc, run, relationship);
        relationship.GetWoundState("wound.reversible").Should().NotBe(WoundState.Rompu);
        relationship.GetWoundState("wound.irreversible").Should().Be(WoundState.Rompu);
    }

    [Fact]
    public async Task ApplyRewardCurseEffect_ShouldCoverHealDamageAndUnsupportedBranches()
    {
        var resolver = Resolver(out _, out _);
        var run = TestGameEngineFactory.CreateRun();
        var startingHp = run.CurrentHp;

        (await Apply(resolver, Entry(resultKind: "Heal", amount: 0), run)).Should().BeNull();
        var heal = await Apply(resolver, Entry(resultKind: "Heal", amount: 5), run);
        heal.Should().NotBeNull();
        heal!.Kind.Should().Be("heal");

        (await Apply(resolver, Entry(resultKind: "Damage", amount: 0), run)).Should().BeNull();
        var damage = await Apply(resolver, Entry(resultKind: "Damage", amount: 3), run);
        damage.Should().NotBeNull();
        damage!.Kind.Should().Be("damage");
        run.CurrentHp.Should().BeLessThanOrEqualTo(startingHp);

        (await Apply(resolver, Entry(resultKind: "Unknown"), run)).Should().BeNull();
    }

    [Fact]
    public async Task ApplyRewardCurseEffect_ShouldCoverMissingAndFailedCurseAndLawLookups()
    {
        var resolver = Resolver(out var catalog, out _);
        var run = TestGameEngineFactory.CreateRun();

        (await Apply(resolver, Entry(resultKind: "GrantCurse", targetKey: " "), run)).Should().BeNull();
        (await Apply(resolver, Entry(resultKind: "GrantLaw", targetKey: null), run)).Should().BeNull();

        catalog.Setup(gateway => gateway.GetCurseDefinitionByKeyAsync("curse.missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogCurseDefinitionSnapshot>.Failure(
                Error.Create("catalog.missing", "missing")));
        catalog.Setup(gateway => gateway.GetPalaceLawDefinitionByKeyAsync("law.missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PalaceLawDefinitionSnapshot>.Failure(
                Error.Create("catalog.missing", "missing")));

        (await Apply(resolver, Entry(resultKind: "GrantCurse", targetKey: "curse.missing"), run)).Should().BeNull();
        (await Apply(resolver, Entry(resultKind: "GrantLaw", targetKey: "law.missing"), run)).Should().BeNull();
    }

    private static NpcEventChoiceResolver Resolver(
        out Mock<ICatalogContentGateway> catalog,
        out Mock<IPlayerProfileGateway> player)
    {
        catalog = new Mock<ICatalogContentGateway>();
        player = new Mock<IPlayerProfileGateway>();
        return new NpcEventChoiceResolver(catalog.Object, player.Object);
    }

    private static CatalogNpcDefinition Npc(IReadOnlyCollection<CatalogNpcWound>? wounds) =>
        new("npc.test", "NPC", "Description", [], [], [], [], Wounds: wounds);

    private static CatalogRewardCurseEntry Entry(
        IReadOnlyCollection<CatalogRewardCurseAvailability>? availability = null,
        string resultKind = "Heal",
        int amount = 1,
        string? targetKey = null) =>
        new("Reward", resultKind, targetKey, amount, availability!);

    private static CatalogRewardCurseAvailability Gate(string kind, int value) => new(kind, value);

    private static bool Available(CatalogRewardCurseEntry entry, Run run) =>
        (bool)IsAvailableMethod.Invoke(null, [entry, run, null])!;

    private static void Invoke(MethodInfo method, params object?[] args) => method.Invoke(null, args);

    private static async Task<AppliedConsequenceEffect?> Apply(
        NpcEventChoiceResolver resolver,
        CatalogRewardCurseEntry entry,
        Run run)
    {
        var task = (Task<AppliedConsequenceEffect?>)ApplyRewardCurseEffectMethod.Invoke(
            resolver, [entry, run, CancellationToken.None])!;
        return await task;
    }

    private static MethodInfo PrivateStatic(string name) =>
        typeof(NpcEventChoiceResolver).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"{name} was not found.");
}
