using Leds.Catalog.Application.RoomBosses.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.RoomBosses;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryRoomBossDefinitionReadStore : IRoomBossDefinitionReadStore
{
    private readonly IReadOnlyCollection<IRoomBossDefinition> _definitions;

    public InMemoryRoomBossDefinitionReadStore()
    {
        _definitions =
        [
            CreateWardenOfThreshold(),
            CreateRootboundMemory(),
            CreateFracturedEcho(),
            CreateMuteHerald(),
            CreateLastDoor(),
            CreateArchivist(),
            CreateHimlit()
        ];
    }

    public Task<IReadOnlyCollection<IRoomBossDefinition>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(definition => definition.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IRoomBossDefinition>>(definitions);
    }

    public Task<IRoomBossDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(definition =>
            string.Equals(
                definition.Key.Value,
                key,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    public Task<IRoomBossDefinition?> GetByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(definition =>
            string.Equals(
                definition.RoomType,
                roomType,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    private static RoomBossDefinition CreateWardenOfThreshold()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.threshold.warden",
            name: "Warden of the Threshold",
            description: "The first sentinel guarding the entrance to the Memory Palace, a being of pure vigilance.",
            version: "1.0.0",
            roomType: "Threshold",
            baseDifficulty: 1,
            tags: ["sentinel", "guardian", "threshold"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateRootboundMemory()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.forest.rootbound-memory",
            name: "Rootbound Memory",
            description: "An ancient entity whose roots dig deep into forgotten epochs, anchoring the forest's will.",
            version: "1.0.0",
            roomType: "Forest",
            baseDifficulty: 2,
            tags: ["ancient", "forest", "roots"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateFracturedEcho()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.rupture.fractured-echo",
            name: "Fractured Echo",
            description: "A shattered remnant of a once-coherent thought, now a cacophony of broken memories.",
            version: "1.0.0",
            roomType: "Rupture",
            baseDifficulty: 3,
            tags: ["shattered", "echo", "rupture"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateMuteHerald()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.silence.mute-herald",
            name: "Mute Herald",
            description: "A silent messenger whose presence absorbs all sound, leaving only dread in the void.",
            version: "1.0.0",
            roomType: "Silence",
            baseDifficulty: 4,
            tags: ["silent", "herald", "void"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateLastDoor()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.antechamber.last-door",
            name: "The Last Door",
            description: "The final barrier before the deepest memories, an immovable ward of immense presence.",
            version: "1.0.0",
            roomType: "Antechamber",
            baseDifficulty: 5,
            tags: ["barrier", "ward", "antechamber"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateArchivist()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.memory.archivist",
            name: "Archivist of Lost Moments",
            description: "The keeper of forgotten memories, cataloging every experience that slips through the cracks of time.",
            version: "1.0.0",
            roomType: "Memory",
            baseDifficulty: 4,
            tags: ["archivist", "memory", "keeper"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }

    private static RoomBossDefinition CreateHimlit()
    {
        var def = RoomBossDefinition.Create(
            key: "boss.final.himlit",
            name: "Himlit",
            description: "The final, eldritch entity at the heart of the Memory Palace. Its true name is a whisper lost in the abyss of ultimate remembrance.",
            version: "1.0.0",
            roomType: "Final",
            baseDifficulty: 10,
            tags: ["final", "eldritch", "heart"],
            status: CatalogContentStatus.Draft);

        def.Activate();

        return def;
    }
}
