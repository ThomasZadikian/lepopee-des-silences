using FluentAssertions;
using Leds.GameEngine.Application.Combats.Tactical;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

public sealed class TacticalEquipmentAbilityCatalogTests
{
    [Theory]
    [InlineData("tactical-mind-control", "item.iris-amethyste", "Iris améthyste", "SingleEnemy")]
    [InlineData("tactical-temporal-slow", "item.aiguille-arret", "Aiguille d'arrêt", "AllEnemies")]
    [InlineData("tactical-extend-periodic-duration", "item.aiguille-relieur", "Aiguille du Relieur", "SingleEnemy")]
    public void TryResolve_ShouldResolveEverySupportedAbility_WhenEquipped(
        string behaviorCode,
        string itemKey,
        string expectedName,
        string expectedTargeting)
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = [$"behavior:{behaviorCode}|{itemKey}"]
            });

        var resolved = TacticalEquipmentAbilityCatalog.TryResolve(
            combat, ally.Id.Value, ComputeAbilityId(ally.Id.Value, itemKey), out var ability);

        resolved.Should().BeTrue();
        ability.DisplayName.Should().Be(expectedName);
        ability.TargetingType.Should().Be(expectedTargeting);
        ability.BehaviorCode.Should().Be(behaviorCode);
        ability.ItemKey.Should().Be(itemKey);
        ability.UseKey.Should().Be($"equipment:{behaviorCode}");
    }

    [Fact]
    public void TryResolve_ShouldFail_WhenAbilityIsNotEquipped()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);

        var resolved = TacticalEquipmentAbilityCatalog.TryResolve(
            combat, ally.Id.Value, ComputeAbilityId(ally.Id.Value, "item.iris-amethyste"), out _);

        resolved.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_ShouldFail_WhenEquippedAbilityIdDoesNotMatch()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["behavior:tactical-mind-control|item.iris-amethyste"]
            });

        TacticalEquipmentAbilityCatalog.TryResolve(combat, ally.Id.Value, Guid.NewGuid(), out _)
            .Should().BeFalse();
    }

    [Fact]
    public void GetUsable_ShouldExposeAllEquippedAbilitiesForActiveCombatant()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var tokens = new[]
        {
            "behavior:tactical-mind-control|item.iris-amethyste",
            "behavior:tactical-temporal-slow|item.aiguille-arret",
            "behavior:tactical-extend-periodic-duration|item.aiguille-relieur"
        };
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = tokens,
                [enemy.Id.Value] = tokens
            });

        var result = TacticalEquipmentAbilityCatalog.GetUsable(combat);

        result.Should().HaveCount(3);
        result.Select(x => x.DisplayName).Should().BeEquivalentTo(
            ["Iris améthyste", "Aiguille d'arrêt", "Aiguille du Relieur"]);
        result.Should().OnlyContain(x => x.EffectType == "EquipmentAbility" && x.Quantity == 1);
    }

    [Fact]
    public void GetUsable_ShouldHideAbilityAlreadyUsedOnce()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var tokens = new[]
        {
            "behavior:tactical-mind-control|item.iris-amethyste",
            "behavior:tactical-temporal-slow|item.aiguille-arret"
        };
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = tokens,
                [enemy.Id.Value] = tokens
            });
        combat.MarkOnceSkillUsed("equipment:tactical-mind-control");

        var result = TacticalEquipmentAbilityCatalog.GetUsable(combat);

        result.Should().ContainSingle(x => x.DisplayName == "Aiguille d'arrêt");
        result.Should().NotContain(x => x.DisplayName == "Iris améthyste");
    }

    [Fact]
    public void GetUsable_ShouldReturnEmpty_WhenCombatHasNoActiveCombatant()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);

        enemy.ApplyDamage(enemy.CurrentVitality);
        combat.OnCombatantDefeated(enemy.Id.Value);
        ally.ApplyDamage(ally.CurrentVitality);
        combat.OnCombatantDefeated(ally.Id.Value);

        combat.ActiveCombatantId.Should().BeNull();
        TacticalEquipmentAbilityCatalog.GetUsable(combat).Should().BeEmpty();
    }

    private static Guid ComputeAbilityId(Guid actorId, string itemKey)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{actorId:N}:{itemKey}"));
        return new Guid(bytes[..16]);
    }
}
