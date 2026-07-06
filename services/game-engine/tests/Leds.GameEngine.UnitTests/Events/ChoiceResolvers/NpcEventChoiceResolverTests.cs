using FluentAssertions;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Events.ChoiceResolvers;

public sealed class NpcEventChoiceResolverTests
{
    private const string OfferingGiverKey = "npc.test.offering-giver";

    private static (Run Run, MapNode Node) CreateRunWithActiveOfferingGiver()
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Npc);
        var run = runWithNode.Run;

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0);
        var character = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(), definitionKey: "character.player.self",
            displayName: "Le Porteur", statBlock: statBlock, skills: []);
        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            playerId: run.PlayerId, displayName: "Joueur",
            characters: [character], createdAtUtc: DateTimeOffset.UtcNow));

        run.BeginOrResumeNpcEncounter(OfferingGiverKey);

        return (run, runWithNode.TargetNode);
    }

    [Fact]
    public void EventType_ShouldReturnNpc()
    {
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

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

        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

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

        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.Message.Should().Contain("efface");
        result.EncounterCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_ShouldUnlockSkillAndClaimOffering_WhenMajorSkillOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-skill");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.UnlockedSkills.Should().ContainSingle(
            u => u.PlayerId == run.PlayerId && u.SkillKey == "skill.basic.strike" && u.Source == $"npc:{OfferingGiverKey}");
        playerProfileGateway.ClaimedOfferings.Should().ContainSingle(
            c => c.NpcKey == OfferingGiverKey && c.OfferingKey == "offer.skill");
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotRegrantMajorOffering_WhenAlreadyClaimed()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-skill");
        var playerProfileGateway = new StubPlayerProfileGateway();
        playerProfileGateway.SeedClaimedOffering(run.PlayerId, OfferingGiverKey, "offer.skill");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().ContainSingle(f => f.Text.Contains("déjà"));
        playerProfileGateway.UnlockedSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldAwardStatPoint_WhenGenericStatPointOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-stat");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.AwardedStatPoints.Should().ContainSingle(a => a.PlayerId == run.PlayerId && a.Amount == 1);
        playerProfileGateway.ClaimedOfferings.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldAddRunItem_WhenGenericItemOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-item");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        run.RunItems.Should().Contain(i => i.DefinitionKey == "item.consumable.minor-heal");
    }

    [Fact]
    public async Task ResolveAsync_ShouldPersistReputationMilestone_WhenMilestoneChoiceIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-milestone");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.GrantedMilestones.Should().ContainSingle(
            m => m.NpcKey == OfferingGiverKey && m.MilestoneKey == "trust-earned");
        run.GetNpcRelationship(OfferingGiverKey)!.HasFlag("trust-earned").Should().BeTrue();
    }
}
