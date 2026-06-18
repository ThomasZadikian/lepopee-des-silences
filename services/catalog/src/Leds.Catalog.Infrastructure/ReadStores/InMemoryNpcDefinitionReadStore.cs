using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryNpcDefinitionReadStore : INpcDefinitionReadStore
{
    private readonly IReadOnlyCollection<INpcDefinition> _definitions;

    public InMemoryNpcDefinitionReadStore()
    {
        _definitions =
        [
            CreateNpcNeutralTraveler(),
            CreateNpcSilentWitness(),
            CreateNpcDesertExile(),
            CreateNpcRageBeggar(),
            CreateNpcRainMemoryKeeper()
        ];
    }

    public Task<IReadOnlyCollection<INpcDefinition>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<INpcDefinition>>(definitions);
    }

    private static INpcDefinition CreateNpcNeutralTraveler()
    {
        return NpcDefinition.Create(
            "npc-neutral-traveler",
            "Voyageur neutre",
            "Un voyageur qui traverse les couloirs sans but apparent.",
            "alpha-0.8.1",
            tags: ["npc", "generic"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: null,
            status: CatalogContentStatus.Active);
    }

    private static INpcDefinition CreateNpcSilentWitness()
    {
        return NpcDefinition.Create(
            "npc-silent-witness",
            "Témoin silencieux",
            "Une silhouette figée qui observe sans un mot.",
            "alpha-0.8.1",
            tags: ["npc", "silent"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: ["Silent"],
            compatibleRoomClimates: null,
            status: CatalogContentStatus.Active);
    }

    private static INpcDefinition CreateNpcDesertExile()
    {
        return NpcDefinition.Create(
            "npc-desert-exile",
            "Exilé du désert",
            "Un être brûlé par une chaleur surnaturelle, cherchant l'ombre.",
            "alpha-0.8.1",
            tags: ["npc", "exile"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: ["Heatwave"],
            status: CatalogContentStatus.Active);
    }

    private static INpcDefinition CreateNpcRageBeggar()
    {
        return NpcDefinition.Create(
            "npc-rage-beggar",
            "Mendiant de rage",
            "Une créature secouée par une colère ancienne qui ne s'éteint jamais.",
            "alpha-0.8.1",
            tags: ["npc", "enraged"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: ["Enraged"],
            compatibleRoomClimates: null,
            status: CatalogContentStatus.Active);
    }

    private static INpcDefinition CreateNpcRainMemoryKeeper()
    {
        return NpcDefinition.Create(
            "npc-rain-memory-keeper",
            "Gardien des mémoires de pluie",
            "Un archiviste dont les souvenirs s'égouttent comme une averse incessante.",
            "alpha-0.8.1",
            tags: ["npc", "rain"],
            compatibleRoomTypes: null,
            compatiblePalaceRoomStates: null,
            compatibleRoomClimates: ["Rain"],
            status: CatalogContentStatus.Active);
    }
}
