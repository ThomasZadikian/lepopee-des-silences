using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunDeepBranchCoverageTests
{
    [Fact]
    public void GrantGrimoireSkill_ShouldCoverValidationReplacementProtagonistAndCompanionPaths()
    {
        var run = CreateRunWithTwoCharacters(out var protagonistId, out var companionId);
        var runtime = RuntimeSkill("skill.temp.deluge-mineur");
        var snapshot = SnapshotSkill("skill.temp.deluge-mineur");

        Assert.Throws<DomainException>(() => run.GrantGrimoireSkill(
            new RunItemId(Guid.NewGuid()), protagonistId, runtime, snapshot));

        var wrongType = Item("item.not-grimoire", RunItemType.Consumable, RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(wrongType);
        Assert.Throws<DomainException>(() => run.GrantGrimoireSkill(wrongType.Id, protagonistId, runtime, snapshot));

        var wrongEffect = Item("item.grimoire-wrong-effect", RunItemType.Grimoire, RunItemEffectType.Heal, 1);
        run.AddRunItem(wrongEffect);
        Assert.Throws<DomainException>(() => run.GrantGrimoireSkill(wrongEffect.Id, protagonistId, runtime, snapshot));

        var missingCharacterItem = Item("item.grimoire-missing-character", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(missingCharacterItem);
        Assert.Throws<DomainException>(() => run.GrantGrimoireSkill(
            missingCharacterItem.Id, Guid.NewGuid(), runtime, snapshot));

        var protagonistGrimoire = Item("item.grimoire-protagonist", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 2);
        run.AddRunItem(protagonistGrimoire);
        run.GrantGrimoireSkill(protagonistGrimoire.Id, protagonistId, runtime, snapshot).Should().BeFalse();
        run.PlayerState.Skills.Should().Contain(skill => skill.Key == runtime.Key);

        var companionGrimoire = Item("item.grimoire-companion", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(companionGrimoire);
        run.GrantGrimoireSkill(companionGrimoire.Id, companionId,
                RuntimeSkill("skill.temp.ecriture-appliquee"), SnapshotSkill("skill.temp.ecriture-appliquee"))
            .Should().BeTrue();
        run.PlayerSnapshot!.Characters.Single(c => c.CharacterId == companionId).Skills
            .Should().Contain(skill => skill.TemporarySlot == "Grimoire");
    }

    [Fact]
    public void ConsumeGrimoire_ShouldCoverCombatMissingWrongTypeAndStackDepletionPaths()
    {
        var run = CreateRunWithTwoCharacters(out _, out _);
        var item = Item("item.grimoire-stack", RunItemType.Grimoire, RunItemEffectType.GrantTemporarySkill, 2);
        run.AddRunItem(item);
        run.StartTacticalCombat(CreateCombat(run, run.CurrentRoom.Nodes.First().Id));
        Assert.Throws<DomainException>(() => run.ConsumeGrimoire(item.Id));

        SetPrivate(run, "_activeTacticalCombat", null);
        SetProperty(run, nameof(Run.ActiveCombatId), null);
        Assert.Throws<DomainException>(() => run.ConsumeGrimoire(new RunItemId(Guid.NewGuid())));
        var wrong = Item("item.not-grimoire-consume", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        run.AddRunItem(wrong);
        Assert.Throws<DomainException>(() => run.ConsumeGrimoire(wrong.Id));
        run.ConsumeGrimoire(item.Id).Should().BeFalse();
        run.ConsumeGrimoire(item.Id).Should().BeTrue();
    }

    [Fact]
    public void StartTacticalCombat_ShouldCoverEveryGuardAndSuccessPath()
    {
        var run = TestGameEngineFactory.CreateRun();
        var nodeId = run.CurrentRoom.Nodes.First().Id;
        Assert.Throws<DomainException>(() => run.StartTacticalCombat(
            CreateCombat(run, nodeId, new RunId(Guid.NewGuid()))));

        var inactiveCombat = CreateCombat(run, nodeId);
        SetProperty(inactiveCombat, nameof(TacticalCombat.Status), CombatStatus.Completed);
        Assert.Throws<DomainException>(() => run.StartTacticalCombat(inactiveCombat));

        var inactiveRun = TestGameEngineFactory.CreateRun();
        SetProperty(inactiveRun, nameof(Run.Status), RunStatus.Resolved);
        Assert.Throws<DomainException>(() => inactiveRun.StartTacticalCombat(
            CreateCombat(inactiveRun, inactiveRun.CurrentRoom.Nodes.First().Id)));

        var mismatchedActiveIdRun = TestGameEngineFactory.CreateRun();
        SetProperty(mismatchedActiveIdRun, nameof(Run.ActiveCombatId), CombatId.New());
        Assert.Throws<DomainException>(() => mismatchedActiveIdRun.StartTacticalCombat(
            CreateCombat(mismatchedActiveIdRun, mismatchedActiveIdRun.CurrentRoom.Nodes.First().Id)));

        var active = CreateCombat(run, nodeId);
        run.StartTacticalCombat(active);
        run.RequireActiveTacticalCombat().Should().BeSameAs(active);
        Assert.Throws<DomainException>(() => run.StartTacticalCombat(CreateCombat(run, nodeId)));
    }

    [Fact]
    public void CompleteActiveCombatById_ShouldCoverMissingMismatchAndSuccessPaths()
    {
        var emptyRun = TestGameEngineFactory.CreateRun();
        Assert.Throws<DomainException>(() => emptyRun.RequireActiveTacticalCombat());
        Assert.Throws<DomainException>(() => emptyRun.CompleteActiveCombat(CombatId.New()));

        var fixture = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = CreateCombat(fixture.Run, fixture.TargetNode.Id);
        fixture.Run.StartTacticalCombat(combat);
        Assert.Throws<DomainException>(() => fixture.Run.CompleteActiveCombat(CombatId.New()));
        fixture.Run.CompleteActiveCombat(combat.Id);
        fixture.Run.HasActiveCombat.Should().BeFalse();
        fixture.TargetNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void EscapeActiveCombat_ShouldCoverMissingNotEscapedAndEscapedPaths()
    {
        var noCombat = TestGameEngineFactory.CreateRun();
        Assert.Throws<DomainException>(() => noCombat.EscapeActiveCombat());

        var fixture = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = CreateCombat(fixture.Run, fixture.TargetNode.Id, escapePosition: new GridPosition(0, 0));
        fixture.Run.StartTacticalCombat(combat);
        Assert.Throws<DomainException>(() => fixture.Run.EscapeActiveCombat());
        SetProperty(combat, nameof(TacticalCombat.Status), CombatStatus.Escaped);
        fixture.Run.EscapeActiveCombat();
        fixture.Run.HasActiveCombat.Should().BeFalse();
        fixture.TargetNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void AdvanceRoomActors_ShouldCoverNormalAndActiveCombatGuards()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdvanceRoomActors(ActorAdvanceMode.All).Should().NotBeNull();
        run.StartTacticalCombat(CreateCombat(run, run.CurrentRoom.Nodes.First().Id));
        Assert.Throws<DomainException>(() => run.AdvanceRoomActors(ActorAdvanceMode.HostilesOnly));
    }

    [Fact]
    public void SwapGroundItemIntoInventory_ShouldCoverValidationAndSuccessfulModifierTransfer()
    {
        var run = TestGameEngineFactory.CreateRun();
        var held = Item("item.held", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var otherHeld = Item("item.other-held", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var ground = Item("item.ground", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var otherGround = Item("item.other-ground", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        run.AddRunItem(held);
        run.AddRunItem(otherHeld);
        run.DropCombatLootOnGround([ground, otherGround], 0, 0);

        Assert.Throws<DomainException>(() => run.SwapGroundItemIntoInventory(ground.Id, ground.Id));
        Assert.Throws<DomainException>(() => run.SwapGroundItemIntoInventory(new RunItemId(Guid.NewGuid()), held.Id));
        Assert.Throws<DomainException>(() => run.SwapGroundItemIntoInventory(ground.Id, new RunItemId(Guid.NewGuid())));
        Assert.Throws<DomainException>(() => run.SwapGroundItemIntoInventory(held.Id, otherHeld.Id));
        Assert.Throws<DomainException>(() => run.SwapGroundItemIntoInventory(ground.Id, otherGround.Id));

        var grant = RunModifier.Create(RunModifierType.AttackPowerBonus, 1,
            RunModifierDuration.UntilRoomEnds, "RunItem", held.DefinitionKey);
        run.AddRunModifier(grant);
        run.SwapGroundItemIntoInventory(ground.Id, held.Id);
        ground.IsOnGround.Should().BeFalse();
        held.IsOnGround.Should().BeTrue();
        grant.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void TryCollectGroundItem_ShouldCoverStackCapacityAndFreshPickupBranches()
    {
        var run = TestGameEngineFactory.CreateRun();
        var stack = Item("item.stack", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        run.AddRunItem(stack);
        var merge = Item("item.stack", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        merge.PlaceOnGround(run.CurrentRoom.Id.Value, 1, 1);
        AddPersistedItem(run, merge);
        InvokeTryCollect(run, merge).Should().BeTrue();

        var fullRun = TestGameEngineFactory.CreateRun();
        for (var index = 0; index < fullRun.RunItemCapacity; index++)
            fullRun.AddRunItem(Item($"item.capacity.{index}", RunItemType.Consumable, RunItemEffectType.Heal, 1));
        var blocked = Item("item.blocked", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        blocked.PlaceOnGround(fullRun.CurrentRoom.Id.Value, 1, 1);
        AddPersistedItem(fullRun, blocked);
        InvokeTryCollect(fullRun, blocked).Should().BeFalse();

        var freshRun = TestGameEngineFactory.CreateRun();
        var fresh = Item("item.fresh", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        fresh.PlaceOnGround(freshRun.CurrentRoom.Id.Value, 1, 1);
        AddPersistedItem(freshRun, fresh);
        InvokeTryCollect(freshRun, fresh).Should().BeTrue();
        fresh.IsOnGround.Should().BeFalse();
    }

    [Fact]
    public void DebugClearActiveCurse_ShouldConsumeOnlyActiveCurseModifiers()
    {
        var run = TestGameEngineFactory.CreateRun();
        var activeCurse = RunModifier.Create(RunModifierType.AttackPowerBonus, 1,
            RunModifierDuration.UntilRoomEnds, "Curse", "curse.active");
        var consumedCurse = RunModifier.Create(RunModifierType.AttackPowerBonus, 1,
            RunModifierDuration.UntilRoomEnds, "Curse", "curse.consumed");
        consumedCurse.Consume(DateTime.UtcNow.AddMinutes(-1));
        var law = RunModifier.Create(RunModifierType.AttackPowerBonus, 1,
            RunModifierDuration.UntilRoomEnds, "PalaceLaw", "law.active");
        run.AddRunModifier(activeCurse);
        run.AddRunModifier(consumedCurse);
        run.AddRunModifier(law);
        run.DebugClearActiveCurse();
        activeCurse.IsConsumed.Should().BeTrue();
        law.IsConsumed.Should().BeFalse();
    }

    private static Run CreateRunWithTwoCharacters(out Guid protagonistId, out Guid companionId)
    {
        var run = TestGameEngineFactory.CreateRun();
        protagonistId = Guid.NewGuid();
        companionId = Guid.NewGuid();
        var stats = RunCharacterStatSnapshot.Create(100, 12, 8, 0, 10, 10, 5, 20, 0);
        var normal = RunCharacterSkillSnapshot.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Neutral");
        var protagonist = RunCharacterSnapshot.Create(
            protagonistId, "character.player.self", "Porteur", stats,
            [normal, SnapshotSkill("skill.temp.construction-ephemere")],
            emotionalRegisterCode: "Neutral");
        var companion = RunCharacterSnapshot.Create(
            companionId, "character.companion.test", "Compagnon", stats, [normal],
            emotionalRegisterCode: "Neutral");
        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            run.PlayerId, "Joueur", [protagonist, companion], DateTimeOffset.UtcNow));
        return run;
    }

    private static PlayerRuntimeSkill RuntimeSkill(string key) =>
        PlayerRuntimeSkill.Create(key, key, "Damage", "SingleEnemy", "Damage", 0, 0, 12,
            tacticalRange: 2, tacticalAreaShape: "Single", emotionalRegister: "Neutral");

    private static RunCharacterSkillSnapshot SnapshotSkill(string key) =>
        RunCharacterSkillSnapshot.Create(key, key, "Damage", "SingleEnemy", "Damage", 0, 0, 12,
            tacticalRange: 2, tacticalAreaShape: "Single", emotionalRegister: "Neutral", temporarySlot: "Grimoire");

    private static RunItem Item(string key, RunItemType type, RunItemEffectType effectType, int quantity) =>
        RunItem.Create(key, key, "Test", type, default, quantity, effectType, 10);

    private static TacticalCombat CreateCombat(
        Run run, NodeId nodeId, RunId? runId = null, GridPosition? escapePosition = null)
    {
        var battlefield = TacticalBattlefield.Rehydrate(4, 2, new int[8],
            Enumerable.Repeat(true, 8).ToArray(), Enumerable.Repeat(true, 8).ToArray());
        var ally = Combatant.CreateAlly("player.self", "Porteur", "Porteur", 100);
        var enemy = Combatant.CreateEnemy("enemy.test", "Écho", "Bruiser", 50);
        return TacticalCombat.Create(CombatId.New(), runId ?? run.Id, run.CurrentRoom.Id, nodeId,
            battlefield, [(ally, new GridPosition(0, 0))], [(enemy, new GridPosition(3, 1))],
            DateTime.UtcNow, escapePosition: escapePosition,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
    }

    private static bool InvokeTryCollect(Run run, RunItem item)
    {
        var method = typeof(Run).GetMethod("TryCollectGroundItem", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (bool)method.Invoke(run, [item])!;
    }

    private static void AddPersistedItem(Run run, RunItem item)
    {
        var field = typeof(Run).GetField("_runItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ((List<RunItem>)field.GetValue(run)!).Add(item);
    }

    private static void SetPrivate(object instance, string fieldName, object? value) =>
        instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);

    private static void SetProperty(object instance, string propertyName, object? value) =>
        instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(instance, value);
}
