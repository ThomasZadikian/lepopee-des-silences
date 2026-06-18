using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Rooms;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using System.Net.Http.Json;
using System.Text.Json;

namespace Leds.GameEngine.Infrastructure.Catalog;

/// <summary>
/// HTTP implementation of <see cref="ICatalogContentGateway"/>.
/// </summary>
/// <remarks>
/// Room Boss Profiles, Enemy Definitions and Skill Definitions are available
/// through the Catalog Service HTTP API.
///
/// Other content lookups still require the InMemory gateway and deliberately
/// throw <see cref="CatalogGatewayException"/> when this gateway is used.
///
/// Use <c>CatalogGateway:Mode = InMemory</c> for the complete playable local flow.
/// Use <c>CatalogGateway:Mode = Http</c> to validate the Room Boss Definition,
/// Enemy Definition and Skill Definition integration with the Catalog Service.
/// </remarks>
public sealed class HttpCatalogContentGateway : ICatalogContentGateway
{
    private readonly HttpClient _httpClient;

    public HttpCatalogContentGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<EnemyTemplateSnapshot>> GetEnemyTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        throw NotAvailableYet("Enemy templates");
    }

    public Task<Result<SkillTemplateSnapshot>> GetSkillTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        throw NotAvailableYet("Skill templates");
    }

    public Task<Result<ItemTemplateSnapshot>> GetItemTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        throw NotAvailableYet("Item templates");
    }

    public Task<Result<EventTemplateSnapshot>> GetEventTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        throw NotAvailableYet("Event templates");
    }

    public Task<Result<PalaceLawDefinitionSnapshot>> GetPalaceLawDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetPalaceLawDefinitionByKeyCoreAsync(key, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PalaceLawDefinitionSnapshot>> ListActivePalaceLawDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/palace-laws";

        var wrapper = await GetJsonOrNullAsync<ListPalaceLawDefinitionsHttpResponse>(url, cancellationToken);

        return wrapper?.Definitions?
            .Select(MapToPalaceLawDefinitionSnapshot)
            .Where(definition => string.Equals(definition.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(definition => definition.Priority)
            .ThenBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
    }

    private async Task<Result<PalaceLawDefinitionSnapshot>> GetPalaceLawDefinitionByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<PalaceLawDefinitionSnapshot>.Failure(Error.Create(
                "catalog.palace_law_key_required",
                "Palace law definition key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/palace-laws/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetPalaceLawDefinitionByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Definition is null)
        {
            return Result<PalaceLawDefinitionSnapshot>.Failure(Error.Create(
                "catalog.palace_law_definition_not_found",
                $"Palace law definition '{key}' was not found."));
        }

        return Result<PalaceLawDefinitionSnapshot>.Success(
            MapToPalaceLawDefinitionSnapshot(wrapper.Definition));
    }

    public async Task<CatalogRoomBossProfile?> GetRoomBossProfileAsync(
        string roomType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return null;
        }

        var encodedRoomType = Uri.EscapeDataString(roomType.Trim());
        var url = $"/api/v2/catalog/room-boss-definitions/room-type/{encodedRoomType}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        GetRoomBossDefinitionByRoomTypeHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<GetRoomBossDefinitionByRoomTypeHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        var httpDefinition = wrapper?.Definition;

        if (httpDefinition is null)
        {
            return null;
        }

        return new CatalogRoomBossProfile(
            Key: httpDefinition.Key,
            DisplayName: httpDefinition.Name,
            Description: httpDefinition.Description,
            RoomType: httpDefinition.RoomType,
            BaseDifficulty: httpDefinition.BaseDifficulty,
            Tags: httpDefinition.Tags);
    }

    public async Task<CatalogEnemyDefinition?> GetEnemyDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/enemy-definitions/{encodedKey}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        GetEnemyDefinitionByKeyHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<GetEnemyDefinitionByKeyHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        var httpDefinition = wrapper?.Definition;

        if (httpDefinition is null)
        {
            return null;
        }

        return MapToCatalogEnemyDefinition(httpDefinition);
    }

    public async Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListEnemyDefinitionsByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return [];
        }

        var encodedRoomType = Uri.EscapeDataString(roomType.Trim());
        var url = $"/api/v2/catalog/enemy-definitions/room-type/{encodedRoomType}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        ListEnemyDefinitionsHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<ListEnemyDefinitionsHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        return wrapper?.Definitions?
            .Select(MapToCatalogEnemyDefinition)
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListCompatibleEnemyDefinitionsAsync(
        string roomType,
        int riskLevel,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomType))
        {
            return [];
        }

        var encodedRoomType = Uri.EscapeDataString(roomType.Trim());
        var url = $"/api/v2/catalog/enemy-definitions/compatible?roomType={encodedRoomType}&riskLevel={riskLevel}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        ListEnemyDefinitionsHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<ListEnemyDefinitionsHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        return wrapper?.Definitions?
            .Select(MapToCatalogEnemyDefinition)
            .ToArray() ?? [];
    }

    // ── Skill Definitions ─────────────────────────────────────────────

    public async Task<CatalogSkillDefinition?> GetSkillDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/skill-definitions/{encodedKey}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        GetSkillDefinitionByKeyHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<GetSkillDefinitionByKeyHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        var httpDefinition = wrapper?.Definition;

        if (httpDefinition is null)
        {
            return null;
        }

        return MapToCatalogSkillDefinition(httpDefinition);
    }

    public async Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0)
        {
            return [];
        }

        var url = "/api/v2/catalog/skill-definitions/batch/by-keys";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                url, new { keys }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        ListSkillDefinitionsHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<ListSkillDefinitionsHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        return wrapper?.Definitions?
            .Select(MapToCatalogSkillDefinition)
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByTypeAsync(
        string skillType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillType))
        {
            return [];
        }

        var encodedSkillType = Uri.EscapeDataString(skillType.Trim());
        var url = $"/api/v2/catalog/skill-definitions/type/{encodedSkillType}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        ListSkillDefinitionsHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<ListSkillDefinitionsHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        return wrapper?.Definitions?
            .Select(MapToCatalogSkillDefinition)
            .ToArray() ?? [];
    }

    private static CatalogSkillDefinition MapToCatalogSkillDefinition(
        CatalogSkillDefinitionHttpResponse source)
    {
        return new CatalogSkillDefinition(
            Key: source.Key,
            DisplayName: source.Name,
            Description: source.Description,
            SkillType: source.SkillType,
            TargetingType: source.TargetingType,
            EffectType: source.EffectType,
            ManaCost: source.ManaCost,
            ChargeCost: source.ChargeCost,
            BasePower: source.BasePower,
            Tags: source.Tags ?? []);
    }

    private static CatalogEnemyDefinition MapToCatalogEnemyDefinition(
        CatalogEnemyDefinitionHttpResponse source)
    {
        return new CatalogEnemyDefinition(
            Key: source.Key,
            DisplayName: source.Name,
            Description: source.Description,
            Archetype: source.Archetype,
            CompatibleRoomTypes: source.CompatibleRoomTypes,
            BaseDifficulty: source.BaseDifficulty,
            MinRiskLevel: source.MinRiskLevel,
            MaxRiskLevel: source.MaxRiskLevel,
            Tags: source.Tags,
            SkillKeys: source.SkillKeys);
    }

    private static PalaceLawDefinitionSnapshot MapToPalaceLawDefinitionSnapshot(
        CatalogPalaceLawDefinitionHttpResponse source)
    {
        return new PalaceLawDefinitionSnapshot(
            Key: source.Key,
            Name: source.Name,
            Description: source.Description,
            Version: source.Version,
            Status: source.Status,
            Visibility: source.Visibility,
            Priority: source.Priority,
            ImpactDomains: source.ImpactDomains ?? [],
            EffectSetKey: source.EffectSetKey,
            Effects: source.Effects?.Select(MapToCatalogEffectDefinitionSnapshot).ToArray() ?? []);
    }

    public async Task<IReadOnlyCollection<CatalogNpcDefinition>> ListNpcDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/npc-definitions";

        var wrapper = await GetJsonOrNullAsync<ListNpcDefinitionsHttpResponse>(url, cancellationToken);

        return wrapper?.Definitions?
            .Select(MapToCatalogNpcDefinition)
            .ToArray()
            ?? [];
    }

    private static CatalogNpcDefinition MapToCatalogNpcDefinition(
        CatalogNpcDefinitionHttpResponse source)
    {
        return new CatalogNpcDefinition(
            Key: source.Key,
            DisplayName: source.Name,
            Description: source.Description,
            Tags: source.Tags ?? [],
            CompatibleRoomTypes: source.CompatibleRoomTypes ?? [],
            CompatiblePalaceRoomStates: (source.CompatiblePalaceRoomStates ?? [])
                .Select(s => Enum.Parse<PalaceRoomState>(s, ignoreCase: true))
                .ToArray(),
            CompatibleRoomClimates: source.CompatibleRoomClimates ?? [],
            MinDepth: source.MinDepth ?? 0,
            MaxDepth: source.MaxDepth ?? int.MaxValue);
    }

    private static CatalogEffectDefinitionSnapshot MapToCatalogEffectDefinitionSnapshot(
        CatalogEffectDefinitionHttpResponse source)
    {
        return new CatalogEffectDefinitionSnapshot(
            source.EffectType,
            source.TargetScope,
            source.Value,
            source.ValueMode,
            source.Duration,
            source.StackPolicy,
            source.Condition,
            source.Order,
            source.BehaviorTag,
            source.GenerationTag,
            source.SelectionGroup);
    }

    private static CatalogGatewayException NotAvailableYet(string contentType)
    {
        return new CatalogGatewayException(
            $"{contentType} are not available via the HTTP catalog gateway yet. " +
            "Use CatalogGateway:Mode = InMemory for the complete playable flow.");
    }

    private async Task<T?> GetJsonOrNullAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CatalogGatewayException(
                $"Failed to call Catalog Service at '{url}': {ex.Message}", ex);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CatalogGatewayException(
                $"Catalog Service returned {(int)response.StatusCode} for '{url}': {body}");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }
    }

    private sealed record GetRoomBossDefinitionByRoomTypeHttpResponse(
        CatalogRoomBossDefinitionHttpResponse? Definition);

    private sealed record GetEnemyDefinitionByKeyHttpResponse(
        CatalogEnemyDefinitionHttpResponse? Definition);

    private sealed record ListEnemyDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogEnemyDefinitionHttpResponse>? Definitions);

    private sealed record GetSkillDefinitionByKeyHttpResponse(
        CatalogSkillDefinitionHttpResponse? Definition);

    private sealed record ListSkillDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogSkillDefinitionHttpResponse>? Definitions);

    private sealed record GetPalaceLawDefinitionByKeyHttpResponse(
        CatalogPalaceLawDefinitionHttpResponse? Definition);

    private sealed record ListPalaceLawDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogPalaceLawDefinitionHttpResponse>? Definitions);

    private sealed record CatalogPalaceLawDefinitionHttpResponse(
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Visibility,
        int Priority,
        IReadOnlyCollection<string>? ImpactDomains,
        string? EffectSetKey,
        IReadOnlyCollection<CatalogEffectDefinitionHttpResponse>? Effects);

    private sealed record CatalogEffectDefinitionHttpResponse(
        string EffectType,
        string TargetScope,
        decimal Value,
        string ValueMode,
        string Duration,
        string StackPolicy,
        string? Condition,
        int Order,
        string? BehaviorTag,
        string? GenerationTag,
        string? SelectionGroup);

    private sealed record ListNpcDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogNpcDefinitionHttpResponse>? Definitions);

    private sealed record CatalogNpcDefinitionHttpResponse(
        string Key,
        string Name,
        string Description,
        IReadOnlyCollection<string>? Tags,
        IReadOnlyCollection<string>? CompatibleRoomTypes,
        IReadOnlyCollection<string>? CompatiblePalaceRoomStates,
        IReadOnlyCollection<string>? CompatibleRoomClimates,
        int? MinDepth,
        int? MaxDepth);
}
