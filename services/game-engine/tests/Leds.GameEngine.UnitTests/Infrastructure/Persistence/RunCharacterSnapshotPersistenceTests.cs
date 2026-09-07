using FluentAssertions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;

namespace Leds.GameEngine.UnitTests.Infrastructure.Persistence;

public sealed class RunCharacterSnapshotPersistenceTests
{
    [Fact]
    public void Character_emotional_register_should_be_copied_to_persistence_entity()
    {
        var snapshot = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(),
            definitionKey: "character.thomas",
            displayName: "Thomas",
            statBlock: RunCharacterStatSnapshot.CreateDefault(),
            skills: [],
            emotionalRegisterCode: "silence");

        var entity = RunPersistenceMapper.ToCharacterSnapshotEntity(snapshot, order: 0);

        entity.EmotionalRegisterCode.Should().Be("silence");
    }

    [Fact]
    public void Current_party_resources_should_round_trip_through_persistence_mapping()
    {
        var snapshot = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(),
            definitionKey: "character.companion",
            displayName: "Companion",
            statBlock: RunCharacterStatSnapshot.CreateDefault(),
            skills: [],
            emotionalRegisterCode: "silence");
        snapshot.UpdateCurrentResources(currentVitality: 37, currentMana: 0);

        var entity = RunPersistenceMapper.ToCharacterSnapshotEntity(snapshot, order: 0);

        entity.CurrentVitality.Should().Be(37);
        entity.CurrentMana.Should().Be(0);
    }

    [Fact]
    public void Instance_equipment_loadout_should_be_serialized_with_positions()
    {
        var instanceId = Guid.NewGuid();
        var snapshot = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(),
            definitionKey: "character.porteur",
            displayName: "Porteur",
            statBlock: RunCharacterStatSnapshot.CreateDefault(),
            skills: [],
            emotionalRegisterCode: "silence",
            equipmentLoadout: [new RunEquipmentAssignment(Guid.NewGuid(), instanceId, "item.ring", "Ring2")]);

        var entity = RunPersistenceMapper.ToCharacterSnapshotEntity(snapshot, order: 0);

        entity.EquipmentLoadoutJson.Should().Contain(instanceId.ToString());
        entity.EquipmentLoadoutJson.Should().Contain("Ring2");
    }
}
