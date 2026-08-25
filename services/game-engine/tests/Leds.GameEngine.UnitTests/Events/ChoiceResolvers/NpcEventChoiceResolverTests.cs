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
            speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0);
        var character = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(), definitionKey: "character.player.self",
            displayName: "Le Porteur", statBlock: statBlock, skills: [],
            emotionalRegisterCode: "Neutral");
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
    public async Task ResolveAsync_ShouldRejectLegacyStatPointOfferingWithoutClaimingIt()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-stat");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().ContainSingle(f =>
            f.Text.Contains("progression canonique"));
        playerProfileGateway.ClaimedOfferings.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldAwardCurrency_WhenGenericCurrencyOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-currency");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.AwardedCurrency.Should()
            .ContainSingle(a => a.PlayerId == run.PlayerId && a.Amount == 100);
    }

    // "Loi du Prêteur" (law.preteur): currency gains from NPC offerings are boosted
    // while the law is active.
    [Fact]
    public async Task ResolveAsync_ShouldBoostAwardedCurrency_WhenPreteurIsActive()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        run.AddRunModifier(RunModifier.Create(
            RunModifierType.CurrencyGainBonusPercent, 50, RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw", sourceKey: "law.preteur"));
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-currency");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.AwardedCurrency.Should()
            .ContainSingle(a => a.PlayerId == run.PlayerId && a.Amount == 150);
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotInventPermanentProgressionForResolvedChoice()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-milestone");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().NotContain(f =>
            f.Text.Contains("point de compétence", StringComparison.OrdinalIgnoreCase));
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
    public async Task ResolveAsync_ShouldGrantItem_WhenCatalogTypeAndRarityHaveNoEngineEquivalent()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-nonstandard-item");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        run.RunItems.Should().Contain(i => i.DefinitionKey == "item.equipment.sac-nonstandard");
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

    [Fact]
    public async Task ResolveAsync_ShouldNotGrantOffering_WhenRelationshipScoreBelowRequiredThreshold()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-gated-skill");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().ContainSingle(f => f.Text.Contains("Rien ne se produit"));
        playerProfileGateway.UnlockedSkills.Should().BeEmpty();
        playerProfileGateway.ClaimedOfferings.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldApplyIntrinsicEffect_WhenGrantedItemCarriesEffectRunType()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-item");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        run.RunItems.Should().Contain(i =>
            i.DefinitionKey == "item.consumable.minor-heal"
            && i.EffectType == RunItemEffectType.Heal
            && i.EffectAmount == 25);
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotGrantOffering_WhenPlayerHasNoContainer()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-container-gated-item");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        result.NarrativeFragments.Should().ContainSingle(f => f.Text.Contains("Rien ne se produit"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldGrantOffering_WhenPlayerHasContainer()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        run.AddRunItem(RunItem.Create(
            "canon.item.fiole-cristal", "Fiole de cristal", "Un récipient.",
            RunItemType.Passive, RunItemRarity.Common, quantity: 1,
            RunItemEffectType.None, effectAmount: 0, isContainer: true, containerCapacity: 1));
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-container-gated-item");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        run.RunItems.Should().Contain(i =>
            i.DefinitionKey == "item.consumable.minor-heal"
            && i.EffectType == RunItemEffectType.Heal);
    }

    [Fact]
    public async Task ResolveAsync_ShouldBoostOtherNpcRelationship_WhenReputationBoostOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-reputation");
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), new StubPlayerProfileGateway());

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        run.GetNpcRelationship("npc.other").Should().NotBeNull();
        run.GetNpcRelationship("npc.other")!.RelationshipScore.Should().Be(250);
        // The ReputationBoost offering adjusts npc.other's score, not the offering giver's —
        // it never redirects the active encounter to npc.other. The encounter with the
        // offering giver still ends normally afterward (every stub dialogue choice here has
        // a null NextNodeKey), so ActiveNpcKey clears to null like any other completed choice.
        run.ActiveNpcKey.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldRecruitCompanion_WhenCompanionOfferingIsTaken()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-companion");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.RecruitedCompanions.Should().ContainSingle(
            c => c.PlayerId == run.PlayerId
                && c.CompanionDefinitionKey == "character.test-companion"
                && c.SkillKeys.Contains("skill.basic.guard"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldGrantOffering_WhenRelationshipScoreMeetsRequiredThreshold()
    {
        var (run, node) = CreateRunWithActiveOfferingGiver();
        run.GetNpcRelationship(OfferingGiverKey)!.AdjustScore(5);
        var context = new CurrentEventChoiceResolutionContext(run, run.CurrentRoom, node, "take-gated-skill");
        var playerProfileGateway = new StubPlayerProfileGateway();
        var sut = new NpcEventChoiceResolver(new StubCatalogContentGateway(), playerProfileGateway);

        var result = await sut.ResolveAsync(context);

        result.Accepted.Should().BeTrue();
        playerProfileGateway.UnlockedSkills.Should().ContainSingle(
            u => u.PlayerId == run.PlayerId && u.SkillKey == "skill.basic.strike" && u.Source == $"npc:{OfferingGiverKey}");
        playerProfileGateway.ClaimedOfferings.Should().ContainSingle(
            c => c.NpcKey == OfferingGiverKey && c.OfferingKey == "offer.skill.gated");
    }
}
