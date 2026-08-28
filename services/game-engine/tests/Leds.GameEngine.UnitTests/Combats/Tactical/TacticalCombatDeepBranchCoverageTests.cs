using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalCombatDeepBranchCoverageTests
{
    [Fact]
    public void TurnStateAndEscapeProperties_ShouldCoverBothBooleanOutcomes()
    {
        new TacticalCombat.TacticalTurnState(false, false).IsSpent.Should().BeFalse();
        new TacticalCombat.TacticalTurnState(true, false).IsSpent.Should().BeFalse();
        new TacticalCombat.TacticalTurnState(false, true).IsSpent.Should().BeFalse();
        new TacticalCombat.TacticalTurnState(true, true).IsSpent.Should().BeTrue();

        CreateCombat().CanEscape.Should().BeFalse();
        var escapable = CreateCombat(escapePosition: new GridPosition(0, 1));
        escapable.CanEscape.Should().BeTrue();
        var completed = RehydrateFrom(escapable, status: CombatStatus.Completed);
        completed.CanEscape.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRejectEmptyOversizedBlockedAndDuplicateDeployments()
    {
        var field = Battlefield(3, 2);
        var ally = Ally("a");
        var enemy = Enemy("e");
        Assert.Throws<DomainException>(() => TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), field,
            [], [(enemy, new GridPosition(2, 1))], DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create()));
        Assert.Throws<DomainException>(() => TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), field,
            [(ally, new GridPosition(0, 0))], [], DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create()));

        var oversized = Enumerable.Range(0, Run.MaxPartySize + 1)
            .Select(index => (Ally($"ally-{index}"), new GridPosition(index % 3, index / 3)))
            .ToArray();
        Assert.Throws<DomainException>(() => TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), Battlefield(4, 4),
            oversized, [(Enemy("target"), new GridPosition(3, 3))], DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create()));

        var blocked = TacticalBattlefield.Rehydrate(2, 1, [0, 0], [false, true], [true, true]);
        Assert.Throws<DomainException>(() => TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), blocked,
            [(Ally("blocked"), new GridPosition(0, 0))],
            [(Enemy("ok"), new GridPosition(1, 0))], DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create()));

        Assert.Throws<DomainException>(() => TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), field,
            [(Ally("duplicate-a"), new GridPosition(0, 0))],
            [(Enemy("duplicate-e"), new GridPosition(0, 0))], DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create()));
    }

    [Fact]
    public void Rehydrate_ShouldNormalizeOptionalCollectionsCooldownsAndActivationCounts()
    {
        var original = CreateCombat();
        var ally = original.Allies.Single();
        var enemy = original.Enemies.Single();
        var unknown = Guid.NewGuid();
        var restored = TacticalCombat.Rehydrate(
            original.Id, original.RunId, original.RoomId, original.NodeId, original.Battlefield,
            [ally], [enemy],
            new Dictionary<Guid, GridPosition>
            {
                [ally.Id.Value] = original.PositionOf(ally.Id.Value),
                [enemy.Id.Value] = original.PositionOf(enemy.Id.Value)
            },
            new Dictionary<Guid, TacticalCombat.TacticalTurnState>
            {
                [ally.Id.Value] = new(true, false)
            },
            [ally.Id.Value, enemy.Id.Value], 0, 2, CombatStatus.Active, DateTime.UtcNow,
            usedOnceSkillKeys: ["", "skill.once"],
            skillCooldowns: new Dictionary<(Guid CombatantId, string SkillKey), int>
            {
                [(ally.Id.Value, "positive")] = 2,
                [(ally.Id.Value, "zero")] = 0,
                [(unknown, "other")] = 3
            },
            activationCounts: new Dictionary<Guid, int> { [ally.Id.Value] = -4, [enemy.Id.Value] = 3 },
            lastActivationUsedMagic: new Dictionary<Guid, bool> { [enemy.Id.Value] = true },
            cannotRevive: [ally.Id.Value],
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());

        restored.HasUsedOnceSkill("skill.once").Should().BeTrue();
        restored.HasUsedOnceSkill("").Should().BeFalse();
        restored.RemainingCooldown(ally.Id.Value, "positive").Should().Be(2);
        restored.RemainingCooldown(ally.Id.Value, "zero").Should().Be(0);
        restored.CooldownsOf(ally.Id.Value).Should().ContainSingle().Which.Key.Should().Be("positive");
        restored.CooldownsOf(enemy.Id.Value).Should().BeEmpty();
        restored.ActivationCounts[ally.Id.Value].Should().Be(0);
        restored.ActivationCounts[enemy.Id.Value].Should().Be(3);
        restored.LastActivationUsedMagic[ally.Id.Value].Should().BeFalse();
        restored.LastActivationUsedMagic[enemy.Id.Value].Should().BeTrue();
        restored.CannotRevive.Should().Contain(ally.Id.Value);
    }

    [Fact]
    public void ReviveNear_ShouldCoverUnknownLivingForbiddenAndSuccessfulRevival()
    {
        var user = Ally("user");
        var fallen = Ally("fallen");
        fallen.MarkDefeated();
        var combat = CreateCombat([user, fallen], [Enemy("enemy")]);

        Assert.Throws<DomainException>(() => combat.ReviveNear(Guid.NewGuid(), user.Id.Value, 10));
        Assert.Throws<DomainException>(() => combat.ReviveNear(user.Id.Value, user.Id.Value, 10));

        var forbidden = RehydrateFrom(combat, cannotRevive: [fallen.Id.Value]);
        Assert.Throws<DomainException>(() => forbidden.ReviveNear(fallen.Id.Value, user.Id.Value, 10));

        var destination = combat.ReviveNear(fallen.Id.Value, user.Id.Value, 25);
        fallen.IsDefeated.Should().BeFalse();
        fallen.CurrentVitality.Should().Be(25);
        destination.Should().NotBe(combat.PositionOf(user.Id.Value));
    }

    [Fact]
    public void MirrorPostDeathAndCombatantLookup_ShouldCoverAllGuards()
    {
        var disabled = CreateCombat();
        var ally = disabled.Allies.Single();
        var enemy = disabled.Enemies.Single();
        disabled.TryConsumeMirrorTrigger(ally).Should().BeFalse();
        disabled.RegisterCombatantDefeated();
        disabled.NextActionRestrictedToBasicAttack.Should().BeFalse();
        disabled.GetFastestLivingEnemy().Should().Be(enemy);
        Assert.Throws<DomainException>(() => disabled.PositionOf(Guid.NewGuid()));

        var enabled = CreateCombat(mirrorEnabled: true, postDeathBasicAttackOnlyEnabled: true);
        var enabledAlly = enabled.Allies.Single();
        var enabledEnemy = enabled.Enemies.Single();
        enabled.TryConsumeMirrorTrigger(enabledEnemy).Should().BeFalse();
        enabled.TryConsumeMirrorTrigger(enabledAlly).Should().BeTrue();
        enabled.TryConsumeMirrorTrigger(enabledAlly).Should().BeFalse();
        enabled.RegisterCombatantDefeated();
        enabled.NextActionRestrictedToBasicAttack.Should().BeTrue();
        enabled.ConsumeBasicAttackRestriction();
        enabled.NextActionRestrictedToBasicAttack.Should().BeFalse();

        enabledEnemy.MarkDefeated();
        enabled.GetFastestLivingEnemy().Should().BeNull();
    }

    [Fact]
    public void OnceSkillAndEquipmentTrigger_ShouldCoverBlankDuplicateAndFirstSecondTriggerPaths()
    {
        var combat = CreateCombat();
        Assert.Throws<DomainException>(() => combat.MarkOnceSkillUsed(" "));
        combat.MarkOnceSkillUsed("skill.once");
        Assert.Throws<DomainException>(() => combat.MarkOnceSkillUsed("skill.once"));
        var actor = combat.Allies.Single().Id.Value;
        combat.TryConsumeEquipmentTrigger(actor, "trigger").Should().BeTrue();
        combat.TryConsumeEquipmentTrigger(actor, "trigger").Should().BeFalse();

        var completed = RehydrateFrom(combat, status: CombatStatus.Completed);
        Assert.Throws<DomainException>(() => completed.MarkOnceSkillUsed("another"));
    }

    private static TacticalCombat CreateCombat(
        GridPosition? escapePosition = null,
        bool mirrorEnabled = false,
        bool postDeathBasicAttackOnlyEnabled = false) =>
        CreateCombat([Ally("ally")], [Enemy("enemy")], escapePosition,
            mirrorEnabled, postDeathBasicAttackOnlyEnabled);

    private static TacticalCombat CreateCombat(
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        GridPosition? escapePosition = null,
        bool mirrorEnabled = false,
        bool postDeathBasicAttackOnlyEnabled = false)
    {
        var field = Battlefield(5, 3);
        var allyPlacements = allies.Select((combatant, index) =>
            (combatant, new GridPosition(index, 0))).ToArray();
        var enemyPlacements = enemies.Select((combatant, index) =>
            (combatant, new GridPosition(4 - index, 2))).ToArray();
        return TacticalCombat.Create(
            CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), field,
            allyPlacements, enemyPlacements, DateTime.UtcNow,
            escapePosition: escapePosition,
            miroirEnabled: mirrorEnabled,
            postDeathBasicAttackOnlyEnabled: postDeathBasicAttackOnlyEnabled,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());
    }

    private static TacticalCombat RehydrateFrom(
        TacticalCombat combat,
        CombatStatus? status = null,
        IReadOnlyCollection<Guid>? cannotRevive = null) =>
        TacticalCombat.Rehydrate(
            combat.Id, combat.RunId, combat.RoomId, combat.NodeId, combat.Battlefield,
            combat.Allies, combat.Enemies, combat.Positions,
            combat.AllCombatants.ToDictionary(c => c.Id.Value, c => combat.TurnStateOf(c.Id.Value)),
            combat.InitiativeOrder, 0, combat.RoundNumber, status ?? combat.Status, combat.CreatedAtUtc,
            escapePosition: combat.EscapePosition,
            cannotRevive: cannotRevive,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());

    private static TacticalBattlefield Battlefield(int width, int height) =>
        TacticalBattlefield.Rehydrate(width, height, new int[width * height],
            Enumerable.Repeat(true, width * height).ToArray(),
            Enumerable.Repeat(true, width * height).ToArray());

    private static Combatant Ally(string key) => Combatant.CreateAlly($"ally.{key}", key, "Hero", 100);
    private static Combatant Enemy(string key) => Combatant.CreateEnemy($"enemy.{key}", key, "Enemy", 100);
}
