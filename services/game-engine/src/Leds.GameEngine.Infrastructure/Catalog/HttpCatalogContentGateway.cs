using System.Net.Http.Json;
using System.Text.Json;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Infrastructure.Catalog;

/// <summary>
/// HTTP implementation of <see cref="ICatalogContentGateway"/>.
/// </summary>
/// <remarks>
/// Room Boss Profiles and Enemy Definitions are available through the
/// Catalog Service HTTP API.
///
/// Other content lookups still require the InMemory gateway and deliberately
/// throw <see cref="CatalogGatewayException"/> when this gateway is used.
///
/// Use <c>CatalogGateway:Mode = InMemory</c> for the complete playable local flow.
/// Use <c>CatalogGateway:Mode = Http</c> to validate the Room Boss Definition
/// and Enemy Definition integration with the Catalog Service.
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
        throw NotAvailableYet("Palace law definitions");
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

    private static CatalogGatewayException NotAvailableYet(string contentType)
    {
        return new CatalogGatewayException(
            $"{contentType} are not available via the HTTP catalog gateway yet. " +
            "Use CatalogGateway:Mode = InMemory for the complete playable flow.");
    }

    private sealed record GetRoomBossDefinitionByRoomTypeHttpResponse(
        CatalogRoomBossDefinitionHttpResponse? Definition);

    private sealed record GetEnemyDefinitionByKeyHttpResponse(
        CatalogEnemyDefinitionHttpResponse? Definition);

    private sealed record ListEnemyDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogEnemyDefinitionHttpResponse>? Definitions);
}
