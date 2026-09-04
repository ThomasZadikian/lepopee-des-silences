using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Players.Equipment;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class EquipmentChangePlannerTests
{
    [Fact]
    public async Task Plan_ShouldUseCatalogSlotAndProficiencyMetadata()
    {
        var setup = Setup(["cloth"]);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Robe", ["Chest"], null, ["cloth"], []));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.CanEquip.Should().BeTrue();
        plan.BlockingReasons.Should().BeEmpty();
    }

    [Fact]
    public async Task Plan_ShouldRejectMissingProficiencyWithoutMutatingLoadout()
    {
        var setup = Setup(["cloth"]);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Armure", ["Chest"], null, ["heavy-armor"], []));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.CanEquip.Should().BeFalse();
        plan.BlockingReasons.Should().Contain("ProficiencyRequirementNotMet");
        setup.Character.EquipmentLoadout.Should().BeEmpty();
    }

    [Fact]
    public async Task Plan_ShouldExposeEffectiveStatsDeltasAndPreserveCurrentResources()
    {
        var setup = Setup([]);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Vitalité", ["Chest"], null, [],
            [
                new("StatBonus", "MaxVitality", 20, null),
                new("StatBonusPercent", "Mana", 10, null)
            ]));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            currentVitality: 70, currentMana: 40, CancellationToken.None);

        plan.ProjectedEffectiveStats.MaxVitality.Should().Be(120);
        plan.ProjectedEffectiveStats.Mana.Should().Be(93);
        plan.ProjectedCurrentVitality.Should().Be(70);
        plan.ProjectedCurrentMana.Should().Be(40);
        plan.StatDeltas.Should().Contain(delta => delta.Stat == "MaxVitality" && delta.Delta == 20);
    }

    private static TestSetup Setup(IReadOnlyCollection<string> proficiencies)
    {
        var now = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", now);
        var archetype = new ArchetypeDefinitionSnapshot(
            "archetype.test", PlayerCharacterStatBlock.CreateDefaultPorteur(), proficiencies,
            [], ["skill.test"], ["skill.test"]);
        var character = profile.CreatePlayableCharacter("Test", archetype, now);
        profile.AddPermanentItems(["item.heavy"], Guid.NewGuid(), now);
        var item = profile.PermanentItems.Single();
        var equipment = new EquipmentGateway();
        var archetypes = new ArchetypeGateway(archetype);
        return new(profile, character, item, equipment,
            new EquipmentChangePlanner(equipment, archetypes));
    }

    private sealed record TestSetup(
        PlayerProfile Profile,
        PlayerCharacter Character,
        PlayerPermanentItem Item,
        EquipmentGateway Equipment,
        EquipmentChangePlanner Planner);

    private sealed class EquipmentGateway : IEquipmentDefinitionGateway
    {
        private readonly Dictionary<string, EquipmentDefinitionSnapshot> _items =
            new(StringComparer.OrdinalIgnoreCase);
        public void Add(EquipmentDefinitionSnapshot item) => _items[item.Key] = item;
        public Task<EquipmentDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(_items.GetValueOrDefault(key));
    }

    private sealed class ArchetypeGateway(ArchetypeDefinitionSnapshot definition)
        : IArchetypeDefinitionGateway
    {
        public Task<ArchetypeDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult<ArchetypeDefinitionSnapshot?>(
                string.Equals(key, definition.Key, StringComparison.OrdinalIgnoreCase) ? definition : null);
    }
}
