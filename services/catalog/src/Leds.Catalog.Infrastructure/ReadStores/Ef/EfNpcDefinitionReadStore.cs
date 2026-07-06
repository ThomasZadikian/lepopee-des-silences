using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Application.Npcs.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Catalog.Infrastructure.ReadStores.Ef;

public sealed class EfNpcDefinitionReadStore : INpcDefinitionReadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _context;

    public EfNpcDefinitionReadStore(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<INpcDefinition>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.NpcDefinitions
            .Where(e => e.Status == "Active")
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToArray();
    }

    private static INpcDefinition MapToDomain(NpcDefinitionEntity entity)
    {
        var tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? [];
        var compatibleRoomTypes = JsonSerializer.Deserialize<List<string>>(entity.CompatibleRoomTypesJson) ?? [];
        var compatiblePalaceRoomStates = JsonSerializer.Deserialize<List<string>>(entity.CompatiblePalaceRoomStatesJson) ?? [];
        var compatibleRoomClimates = JsonSerializer.Deserialize<List<string>>(entity.CompatibleRoomClimatesJson) ?? [];

        var affinity = Enum.TryParse<EmotionalRegister>(entity.EmotionalAffinity, ignoreCase: true, out var parsed)
            ? parsed
            : EmotionalRegister.Neutral;

        var persona = DeserializeOrNull<NpcPersona>(entity.PersonaJson);
        var dialogueGraph = DeserializeOrNull<NpcDialogueGraph>(entity.DialogueGraphJson);
        var wounds = JsonSerializer.Deserialize<List<NpcWound>>(entity.WoundsJson, JsonOptions) ?? [];
        var encounterKeys = JsonSerializer.Deserialize<List<string>>(entity.EncounterKeysJson, JsonOptions) ?? [];
        var boundRoomKeys = JsonSerializer.Deserialize<List<string>>(entity.BoundRoomKeysJson, JsonOptions) ?? [];
        var offerings = JsonSerializer.Deserialize<List<NpcOffering>>(entity.OfferingsJson ?? "[]", JsonOptions) ?? [];

        return NpcDefinition.Create(
            entity.Key,
            entity.Name,
            entity.Description,
            entity.Version,
            tags,
            compatibleRoomTypes,
            compatiblePalaceRoomStates,
            compatibleRoomClimates,
            entity.MinDepth,
            entity.MaxDepth,
            Enum.Parse<CatalogContentStatus>(entity.Status),
            affinity,
            persona,
            dialogueGraph,
            wounds,
            encounterKeys,
            entity.IsRecurring,
            boundRoomKeys,
            offerings);
    }

    private static T? DeserializeOrNull<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}