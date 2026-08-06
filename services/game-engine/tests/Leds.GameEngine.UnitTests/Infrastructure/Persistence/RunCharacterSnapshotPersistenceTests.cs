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
}
