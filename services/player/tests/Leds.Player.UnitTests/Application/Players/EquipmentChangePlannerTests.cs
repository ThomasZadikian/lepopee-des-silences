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

    [Fact]
    public async Task Plan_ShouldBlockItemsThatAreNotOwnedOrNotDefined()
    {
        var setup = Setup([]);

        var notOwned = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, OwnedItemInstanceId.New(), EquipmentPosition.Chest,
            null, null, CancellationToken.None);
        var notDefined = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        notOwned.BlockingReasons.Should().Equal("ItemNotOwned");
        notDefined.BlockingReasons.Should().Equal("ItemDefinitionMissing");
    }

    [Fact]
    public async Task Plan_ShouldReportSlotAndDuplicateAssignmentConflictsOnlyOnce()
    {
        var setup = Setup([]);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Ring", ["Ring"], null, [], []));
        setup.Profile.EquipItem(
            setup.Character.Id, setup.Item.Id, EquipmentPosition.Ring1,
            [EquipmentSlotKind.Ring], DateTimeOffset.UtcNow);

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.CanEquip.Should().BeFalse();
        plan.BlockingReasons.Should().Contain("SlotNotAllowed");
        plan.BlockingReasons.Count(reason => reason == "ItemAlreadyEquippedElsewhere").Should().Be(1);
    }

    [Fact]
    public async Task Plan_ShouldDetectAssignmentToAnotherCharacter()
    {
        var setup = Setup([]);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Chest", ["Chest"], null, [], []));
        setup.Profile.RecruitCompanion(
            "companion.test", "Companion", PlayerCharacterStatBlock.CreateDefaultPorteur(),
            ["skill.companion"], DateTimeOffset.UtcNow);
        var companion = setup.Profile.Roster.Characters.Single(character => character.CharacterType == "Companion");
        setup.Profile.EquipItem(
            companion.Id, setup.Item.Id, EquipmentPosition.Chest,
            [EquipmentSlotKind.Chest], DateTimeOffset.UtcNow);

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.BlockingReasons.Should().Contain("ItemAlreadyEquippedElsewhere");
    }

    [Fact]
    public async Task Plan_ShouldDescribeReplacementAndUniqueGroupConflict()
    {
        var setup = Setup([]);
        setup.Profile.AddPermanentItems(["item.current", "item.conflict"], Guid.NewGuid(), DateTimeOffset.UtcNow);
        var current = setup.Profile.PermanentItems.Single(item => item.ItemDefinitionKey == "item.current");
        var conflict = setup.Profile.PermanentItems.Single(item => item.ItemDefinitionKey == "item.conflict");
        setup.Profile.EquipItem(
            setup.Character.Id, current.Id, EquipmentPosition.Chest,
            [EquipmentSlotKind.Chest], DateTimeOffset.UtcNow);
        setup.Profile.EquipItem(
            setup.Character.Id, conflict.Id, EquipmentPosition.Ring1,
            [EquipmentSlotKind.Ring], DateTimeOffset.UtcNow);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.current", "Current", ["Chest"], null, [], []));
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.conflict", "Conflict", ["Ring"], "unique.armour", [], []));
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Candidate", ["Chest"], "UNIQUE.ARMOUR", [], []));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.CurrentlyEquippedItem.Should().NotBeNull();
        plan.CurrentlyEquippedItem!.ItemInstanceId.Should().Be(current.Id.Value);
        plan.BlockingReasons.Should().Contain("UniqueEquipGroupConflict");
    }

    [Fact]
    public async Task Plan_ShouldBlockMissingCurrentDefinition()
    {
        var setup = Setup([]);
        setup.Profile.AddPermanentItems(["item.unknown"], Guid.NewGuid(), DateTimeOffset.UtcNow);
        var unknown = setup.Profile.PermanentItems.Single(item => item.ItemDefinitionKey == "item.unknown");
        setup.Profile.EquipItem(
            setup.Character.Id, unknown.Id, EquipmentPosition.Ring1,
            [EquipmentSlotKind.Ring], DateTimeOffset.UtcNow);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Candidate", ["Chest"], null, [], []));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            null, null, CancellationToken.None);

        plan.BlockingReasons.Should().Contain("ItemDefinitionMissing");
    }

    [Fact]
    public async Task Plan_ShouldLimitTemporarySkillsAndClampResourcesAndStats()
    {
        var setup = Setup([]);
        setup.Profile.AddPermanentItems(["item.current"], Guid.NewGuid(), DateTimeOffset.UtcNow);
        var current = setup.Profile.PermanentItems.Single(item => item.ItemDefinitionKey == "item.current");
        setup.Profile.EquipItem(
            setup.Character.Id, current.Id, EquipmentPosition.Ring1,
            [EquipmentSlotKind.Ring], DateTimeOffset.UtcNow);
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.current", "Current", ["Ring"], null, [],
            [new("GrantSkill", null, null, "skill.temporary.one")]));
        setup.Equipment.Add(new EquipmentDefinitionSnapshot(
            "item.heavy", "Candidate", ["Chest"], null, [],
            [
                new("GrantSkill", null, null, "skill.temporary.two"),
                new("GrantSkill", null, null, "skill.temporary.three"),
                new("GrantSkill", null, null, PlayerCharacter.BasicSkillKey),
                new("GrantSkill", null, null, " "),
                new("StatBonus", "MaxVitality", -500, null),
                new("StatBonus", "AttackPower", -500, null),
                new("StatBonus", "MagicAttack", 2, null),
                new("StatBonus", "Defense", 2, null),
                new("StatBonus", "MagicDefense", 2, null),
                new("StatBonus", "StartingGuard", 2, null),
                new("StatBonus", "Speed", -500, null),
                new("StatBonus", "Initiative", 2, null),
                new("StatBonus", "Focus", 2, null),
                new("StatBonus", "Mana", -500, null),
                new("StatBonus", "Movement", -500, null),
                new("StatBonusPercent", "Defense", null, null)
            ]));

        var plan = await setup.Planner.PlanAsync(
            setup.Profile, setup.Character.Id, setup.Item.Id, EquipmentPosition.Chest,
            currentVitality: 999, currentMana: 999, CancellationToken.None);

        plan.BlockingReasons.Should().Contain("TemporarySkillCapacityExceeded");
        plan.ProjectedTemporarySkills.Should().BeEquivalentTo(
            ["skill.temporary.one", "skill.temporary.two", "skill.temporary.three"]);
        plan.CurrentVitality.Should().Be(plan.CurrentEffectiveStats.MaxVitality);
        plan.CurrentMana.Should().Be(plan.CurrentEffectiveStats.Mana);
        plan.ProjectedEffectiveStats.MaxVitality.Should().Be(1);
        plan.ProjectedEffectiveStats.AttackPower.Should().Be(0);
        plan.ProjectedEffectiveStats.Speed.Should().Be(1);
        plan.ProjectedEffectiveStats.Mana.Should().Be(0);
        plan.ProjectedEffectiveStats.Movement.Should().Be(1);
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
