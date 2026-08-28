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

        var missingItem = () => run.GrantGrimoireSkill(
            new RunItemId(Guid.NewGuid()), protagonistId, runtime, snapshot);
        missingItem.Should().Throw<DomainException>();

        var wrongType = Item("item.not-grimoire", RunItemType.Consumable, RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(wrongType);
        (() => run.GrantGrimoireSkill(wrongType.Id, protagonistId, runtime, snapshot))
            .Should().Throw<DomainException>();

        var wrongEffect = Item("item.grimoire-wrong-effect", RunItemType.Grimoire, RunItemEffectType.Heal, 1);
        run.AddRunItem(wrongEffect);
        (() => run.GrantGrimoireSkill(wrongEffect.Id, protagonistId, runtime, snapshot))
            .Should().Throw<DomainException>();

        var missingCharacterItem = Item("item.grimoire-missing-character", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(missingCharacterItem);
        (() => run.GrantGrimoireSkill(missingCharacterItem.Id, Guid.NewGuid(), runtime, snapshot))
            .Should().Throw<DomainException>();

        var protagonistGrimoire = Item("item.grimoire-protagonist", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 2);
        run.AddRunItem(protagonistGrimoire);
        run.GrantGrimoireSkill(protagonistGrimoire.Id, protagonistId, runtime, snapshot)
            .Should().BeFalse();
        run.PlayerState.Skills.Should().Contain(skill => skill.Key == runtime.Key);

        var companionGrimoire = Item("item.grimoire-companion", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 1);
        run.AddRunItem(companionGrimoire);
        run.GrantGrimoireSkill(
                companionGrimoire.Id,
                companionId,
                RuntimeSkill("skill.temp.ecriture-appliquee"),
                SnapshotSkill("skill.temp.ecriture-appliquee"))
            .Should().BeTrue();

        run.PlayerSnapshot!.Characters.Single(c => c.CharacterId == companionId).Skills
            .Should().Contain(skill => skill.TemporarySlot == "Grimoire");
    }

    [Fact]
    public void GrantAndConsumeGrimoire_ShouldRejectUseDuringCombatAndConsumeStacksOutsideCombat()
    {
        var run = CreateRunWithTwoCharacters(out var protagonistId, out _);
        var item = Item("item.grimoire-stack", RunItemType.Grimoire,
            RunItemEffectType.GrantTemporarySkill, 2);
        run.AddRunItem(item);

        var combat = CreateCombat(run, run.CurrentRoom.Nodes.First().Id);
        run.StartTacticalCombat(combat);

        (() => run.GrantGrimoireSkill(item.Id, protagonistId,
                RuntimeSkill("skill.temp.souffle-emprunte"),
                SnapshotSkill("skill.temp.souffle-emprunte")))
            .Should().Throw<DomainException>();
        (() => run.ConsumeGrimoire(item.Id)).Should().Throw<DomainException>();

        // Clear the active combat through the legacy id-based resolver on a selected event run is
        // deliberately avoided here: this test only needs the combat guard. Resetting these two
        // persistence-backed fields isolates the grimoire lifecycle branch without changing rules.
        SetPrivate(run, "_activeTacticalCombat", null);
        SetProperty(run, nameof(Run.ActiveCombatId), null);

        (() => run.ConsumeGrimoire(new RunItemId(Guid.NewGuid()))).Should().Throw<DomainException>();
        var wrong = Item("item.not-grimoire-consume", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        run.AddRunItem(wrong);
        (() => run.ConsumeGrimoire(wrong.Id)).Should().Throw<DomainException>();

        run.ConsumeGrimoire(item.Id).Should().BeFalse();
        run.ConsumeGrimoire(item.Id).Should().BeTrue();
    }

    [Fact]
    public void StartTacticalCombat_ShouldCoverEveryGuardAndSuccessPath()
    {
        var run = TestGameEngineFactory.CreateRun();
        var nodeId = run.CurrentRoom.Nodes.First().Id;

        var wrongRunCombat = CreateCombat(run, nodeId, runId: new RunId(Guid.NewGuid()));
        (() => run.StartTacticalCombat(wrongRunCombat)).Should().Throw<DomainException>();

        var inactiveCombat = CreateCombat(run, nodeId);
        SetProperty(inactiveCombat, nameof(TacticalCombat.Status), CombatStatus.Completed);
        (() => run.StartTacticalCombat(inactiveCombat)).Should().Throw<DomainException>();

        var inactiveRun = TestGameEngineFactory.CreateRun();
        SetProperty(inactiveRun, nameof(Run.Status), RunStatus.Resolved);
        SetProperty(inactiveRun, nameof(Run.Outcome), RunOutcome.Abandon);
        (() => inactiveRun.StartTacticalCombat(CreateCombat(inactiveRun, inactiveRun.CurrentRoom.Nodes.First().Id)))
            .Should().Throw<DomainException>();

        var mismatchedActiveIdRun = TestGameEngineFactory.CreateRun();
        SetProperty(mismatchedActiveIdRun, nameof(Run.ActiveCombatId), CombatId.New());
        (() => mismatchedActiveIdRun.StartTacticalCombat(
                CreateCombat(mismatchedActiveIdRun, mismatchedActiveIdRun.CurrentRoom.Nodes.First().Id)))
            .Should().Throw<DomainException>();

        var active = CreateCombat(run, nodeId);
        run.StartTacticalCombat(active);
        run.RequireActiveTacticalCombat().Should().BeSameAs(active);

        var second = CreateCombat(run, nodeId);
        (() => run.StartTacticalCombat(second)).Should().Throw<DomainException>();
    }

    [Fact]
    public void RequireAndCompleteActiveCombatById_ShouldCoverMissingMismatchAndSuccessPaths()
    {
        var emptyRun = TestGameEngineFactory.CreateRun();
        (() => emptyRun.RequireActiveTacticalCombat()).Should().Throw<DomainException>();
        (() => emptyRun.CompleteActiveCombat(CombatId.New())).Should().Throw<DomainException>();

        var fixture = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = CreateCombat(fixture.Run, fixture.TargetNode.Id);
        fixture.Run.StartTacticalCombat(combat);

        (() => fixture.Run.CompleteActiveCombat(CombatId.New())).Should().Throw<DomainException>();
        fixture.Run.CompleteActiveCombat(combat.Id);

        fixture.Run.HasActiveCombat.Should().BeFalse();
        fixture.TargetNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void EscapeActiveCombat_ShouldRejectNonEscapedCombatAndResolveEscapedCombat()
    {
        var noCombat = TestGameEngineFactory.CreateRun();
        (() => noCombat.EscapeActiveCombat()).Should().Throw<DomainException>();

        var fixture = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var combat = CreateCombat(fixture.Run, fixture.TargetNode.Id, escapePosition: new GridPosition(0, 0));
        fixture.Run.StartTacticalCombat(combat);
        (() => fixture.Run.EscapeActiveCombat()).Should().Throw<DomainException>();

        SetProperty(combat, nameof(TacticalCombat.Status), CombatStatus.Escaped);
        fixture.Run.SetPendingRewardOffer(RewardOfferId.New());
        fixture.Run.EscapeActiveCombat();

        fixture.Run.HasActiveCombat.Should().BeFalse();
        fixture.Run.HasPendingRewardOffer.Should().BeFalse();
        fixture.TargetNode.State.Should().Be(NodeState.Resolved);
    }

    [Fact]
    public void AdvanceRoomActors_ShouldCoverNormalAndActiveCombatGuards()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.AdvanceRoomActors(ActorAdvanceMode.Autonomous).Should().NotBeNull();

        run.StartTacticalCombat(CreateCombat(run, run.CurrentRoom.Nodes.First().Id));
        (() => run.AdvanceRoomActors(ActorAdvanceMode.Autonomous)).Should().Throw<DomainException>();
    }

    [Fact]
    public void SwapGroundItemIntoInventory_ShouldCoverEveryValidationAndSuccessfulModifierTransfer()
    {
        var run = TestGameEngineFactory.CreateRun();
        var held = Item("item.held", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var otherHeld = Item("item.other-held", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var ground = Item("item.ground", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        var otherGround = Item("item.other-ground", RunItemType.Consumable, RunItemEffectType.Heal, 1);
        run.AddRunItem(held);
        run.AddRunItem(otherHeld);
        run.DropCombatLootOnGround([ground, otherGround], 0, 0);

        (() => run.SwapGroundItemIntoInventory(ground.Id, ground.Id)).Should().Throw<DomainException>();
        (() => run.SwapGroundItemIntoInventory(new RunItemId(Guid.NewGuid()), held.Id)).Should().Throw<DomainException>();
        (() => run.SwapGroundItemIntoInventory(ground.Id, new RunItemId(Guid.NewGuid()))).Should().Throw<DomainException>();
        (() => run.SwapGroundItemIntoInventory(held.Id, otherHeld.Id)).Should().Throw<DomainException>();
        (() => run.SwapGroundItemIntoInventory(ground.Id, otherGround.Id)).Should().Throw<DomainException>();

        var grant = RunModifier.Create(
            RunModifierType.AttackPowerBonus,
            1,
            RunModifierDuration.UntilRoomEnds,
            sourceType: "RunItem",
            sourceKey: held.DefinitionKey);
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
        {
            fullRun.AddRunItem(Item($"item.capacity.{index}", RunItemType.Consumable, RunItemEffectType.Heal, 1));
        }
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
        var activeCurse = RunModifier.Create(
            RunModifierType.AttackPowerBonus, 1, RunModifierDuration.UntilRoomEnds,
            sourceType: "Curse", sourceKey: "curse.active");
        var consumedCurse = RunModifier.Create(
            RunModifierType.AttackPowerBonus, 1, RunModifierDuration.UntilRoomEnds,
            sourceType: "Curse", sourceKey: "curse.consumed");
        consumedCurse.Consume(DateTime.UtcNow.AddMinutes(-1));
        var law = RunModifier.Create(
            RunModifierType.AttackPowerBonus, 1, RunModifierDuration.UntilRoomEnds,
            sourceType: "PalaceLaw", sourceKey: "law.active");
        run.AddRunModifier(activeCurse);
        run.AddRunModifier(consumedCurse);
        run.AddRunModifier(law);

        run.DebugClearActiveCurse();

        activeCurse.IsConsumed.Should().BeTrue();
        consumedCurse.IsConsumed.Should().BeTrue();
        law.IsConsumed.Should().BeFalse();
    }

    private static Run CreateRunWithTwoCharacters(out Guid protagonistId, out Guid companionId)
    {
        var run = TestGameEngineFactory.CreateRun();
        protagonistId = Guid.NewGuid();
        companionId = Guid.NewGuid();
        var stats = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 8, startingGuard: 0,
            speed: 10, initiative: 10, focus: 5, mana: 20, charge: 0);

        var oldGrimoire = SnapshotSkill("skill.temp.construction-ephemere");
        var normal = RunCharacterSkillSnapshot.Create(
            "skill.basic.strike", "Frappe", "Damage", "SingleEnemy", "Damage",
            0, 0, 10, emotionalRegister: "Neutral");
        var protagonist = RunCharacterSnapshot.Create(
            protagonistId, "character.player.self", "Porteur", stats,
            [normal, oldGrimoire], "Neutral");
        var companion = RunCharacterSnapshot.Create(
            companionId, "character.companion.test", "Compagnon", stats,
            [normal], "Neutral");
        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            run.PlayerId, "Joueur", [protagonist, companion], DateTimeOffset.UtcNow));
        return run;
    }

    private static PlayerRuntimeSkill RuntimeSkill(string key) =>
        PlayerRuntimeSkill.Create(
            key, key, "Damage", "SingleEnemy", "Damage", 0, 0, 12,
            tacticalRange: 2, tacticalAreaShape: "Single", emotionalRegister: "Neutral");

    private static RunCharacterSkillSnapshot SnapshotSkill(string key) =>
        RunCharacterSkillSnapshot.Create(
            key, key, "Damage", "SingleEnemy", "Damage", 0, 0, 12,
            tacticalRange: 2, tacticalAreaShape: "Single", emotionalRegister: "Neutral",
            temporarySlot: "Grimoire");

    private static RunItem Item(
        string key,
        RunItemType type,
        RunItemEffectType effectType,
        int quantity) =>
        RunItem.Create(
            key,
            key,
            "Test",
            type,
            default,
            quantity,
            effectType,
            10);

    private static TacticalCombat CreateCombat(
        Run run,
        NodeId nodeId,
        RunId? runId = null,
        GridPosition? escapePosition = null)
    {
        var battlefield = TacticalBattlefield.Rehydrate(
            width: 4,
            height: 2,
            elevation: new int[8],
            walkable: Enumerable.Repeat(true, 8).ToArray(),
            isFloor: Enumerable.Repeat(true, 8).ToArray());
        var ally = Combatant.CreateAlly("player.self", "Porteur", "Porteur", 100);
        var enemy = Combatant.CreateEnemy("enemy.test", "Écho", "Bruiser", 50);

        return TacticalCombat.Create(
            CombatId.New(),
            runId ?? run.Id,
            run.CurrentRoom.Id,
            nodeId,
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(3, 1))],
            DateTime.UtcNow,
            escapePosition: escapePosition,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
    }

    private static bool InvokeTryCollect(Run run, RunItem item)
    {
        var method = typeof(Run).GetMethod("TryCollectGroundItem", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TryCollectGroundItem was not found.");
        return (bool)method.Invoke(run, [item])!;
    }

    private static void AddPersistedItem(Run run, RunItem item)
    {
        var field = typeof(Run).GetField("_runItems", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("_runItems was not found.");
        ((List<RunItem>)field.GetValue(run)!).Add(item);
    }

    private static void SetPrivate(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{fieldName} was not found.");
        field.SetValue(instance, value);
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{propertyName} was not found.");
        property.SetValue(instance, value);
    }
}
