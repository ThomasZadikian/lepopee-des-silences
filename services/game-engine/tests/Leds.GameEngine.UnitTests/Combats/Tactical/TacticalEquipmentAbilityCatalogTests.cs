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
    [Fact]
    public void TryResolve_ShouldResolveIrisAmethyste_WhenEquipped()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(
            RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy],
            equippedItemKeys: new Dictionary<Guid, IReadOnlyCollection<string>>
            {
                [ally.Id.Value] = ["item.iris-amethyste"]
            });

        var resolved = TacticalEquipmentAbilityCatalog.TryResolve(
            combat, ally.Id.Value, ComputeAbilityId(ally.Id.Value, "item.iris-amethyste"), out var ability);

        resolved.Should().BeTrue();
        ability.DisplayName.Should().Be("Iris améthyste");
        ability.TargetingType.Should().Be("SingleEnemy");
    }

    [Fact]
    public void TryResolve_ShouldFail_WhenIrisAmethysteIsNotEquipped()
    {
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 100);
        var combat = TestTacticalCombatHelper.Create(RunId.New(), RoomId.New(), NodeId.New(), [ally], [enemy]);

        var resolved = TacticalEquipmentAbilityCatalog.TryResolve(
            combat, ally.Id.Value, ComputeAbilityId(ally.Id.Value, "item.iris-amethyste"), out _);

        resolved.Should().BeFalse();
    }

    private static Guid ComputeAbilityId(Guid actorId, string itemKey)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{actorId:N}:{itemKey}"));
        return new Guid(bytes[..16]);
    }
}
