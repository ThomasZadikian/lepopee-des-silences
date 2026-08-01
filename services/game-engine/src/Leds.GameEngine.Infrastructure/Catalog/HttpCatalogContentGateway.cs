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
/// All Catalog content families have full HTTP endpoint support:
/// room boss profiles, palace laws, curses, item definitions, effect sets,
/// reward templates, enemy/skill definitions, NPC definitions, and item templates.
/// </remarks>
public sealed class HttpCatalogContentGateway : ICatalogContentGateway
{
    private readonly HttpClient _httpClient;

    public HttpCatalogContentGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<ItemTemplateSnapshot>> GetItemTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetItemTemplateByKeyCoreAsync(key, cancellationToken);
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

    public Task<Result<CatalogCurseDefinitionSnapshot>> GetCurseDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetCurseDefinitionByKeyCoreAsync(key, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>> ListAvailableCurseDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/curses";

        var wrapper = await GetJsonOrNullAsync<ListCurseDefinitionsHttpResponse>(url, cancellationToken);

        return wrapper?.Definitions?
            .Select(MapToCatalogCurseDefinitionSnapshot)
            .OrderBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
    }

    public Task<Result<CatalogItemDefinitionSnapshot>> GetItemDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetItemDefinitionByKeyCoreAsync(key, cancellationToken);
    }

    public Task<Result<CatalogEffectSetSnapshot>> GetEffectSetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetEffectSetByKeyCoreAsync(key, cancellationToken);
    }

    public Task<Result<CatalogRewardTemplateSnapshot>> GetRewardTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return GetRewardTemplateByKeyCoreAsync(key, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogRewardTemplateSnapshot>> ListEligibleRewardTemplatesAsync(
        RewardTemplateEligibilityContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.SourceType))
        {
            return [];
        }

        var query = new List<string>
        {
            $"sourceType={Uri.EscapeDataString(context.SourceType.Trim())}"
        };

        if (context.Depth.HasValue) query.Add($"depth={context.Depth.Value}");
        if (!string.IsNullOrWhiteSpace(context.CombatTier)) query.Add($"combatTier={Uri.EscapeDataString(context.CombatTier.Trim())}");
        if (context.DifficultyMultiplier.HasValue) query.Add($"difficultyMultiplier={context.DifficultyMultiplier.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (context.RewardPowerMultiplier.HasValue) query.Add($"rewardPowerMultiplier={context.RewardPowerMultiplier.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var url = $"/api/v2/catalog/reward-templates/eligible?{string.Join('&', query)}";
        var wrapper = await GetJsonOrNullAsync<ListRewardTemplatesHttpResponse>(url, cancellationToken);

        return wrapper?.Definitions?
            .Select(MapToCatalogRewardTemplateSnapshot)
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

    private async Task<Result<CatalogCurseDefinitionSnapshot>> GetCurseDefinitionByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<CatalogCurseDefinitionSnapshot>.Failure(Error.Create(
                "catalog.curse_key_required",
                "Curse definition key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/curses/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetCurseDefinitionByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Definition is null)
        {
            return Result<CatalogCurseDefinitionSnapshot>.Failure(Error.Create(
                "catalog.curse_definition_not_found",
                $"Curse definition '{key}' was not found."));
        }

        return Result<CatalogCurseDefinitionSnapshot>.Success(
            MapToCatalogCurseDefinitionSnapshot(wrapper.Definition));
    }

    private async Task<Result<CatalogItemDefinitionSnapshot>> GetItemDefinitionByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create(
                "catalog.item_definition_key_required",
                "Item definition key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/item-definitions/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetItemDefinitionByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Definition is null)
        {
            return Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create(
                "catalog.item_definition_not_found",
                $"Item definition '{key}' was not found."));
        }

        return Result<CatalogItemDefinitionSnapshot>.Success(
            MapToCatalogItemDefinitionSnapshot(wrapper.Definition));
    }

    private async Task<Result<CatalogEffectSetSnapshot>> GetEffectSetByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<CatalogEffectSetSnapshot>.Failure(Error.Create(
                "catalog.effect_set_key_required",
                "Effect set key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/effect-sets/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetEffectSetByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Definition is null)
        {
            return Result<CatalogEffectSetSnapshot>.Failure(Error.Create(
                "catalog.effect_set_not_found",
                $"Effect set '{key}' was not found."));
        }

        return Result<CatalogEffectSetSnapshot>.Success(
            MapToCatalogEffectSetSnapshot(wrapper.Definition));
    }

    private async Task<Result<CatalogRewardTemplateSnapshot>> GetRewardTemplateByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<CatalogRewardTemplateSnapshot>.Failure(Error.Create(
                "catalog.reward_template_key_required",
                "Reward template key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/reward-templates/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetRewardTemplateByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Definition is null)
        {
            return Result<CatalogRewardTemplateSnapshot>.Failure(Error.Create(
                "catalog.reward_template_not_found",
                $"Reward template '{key}' was not found."));
        }

        return Result<CatalogRewardTemplateSnapshot>.Success(
            MapToCatalogRewardTemplateSnapshot(wrapper.Definition));
    }

    // ── Item Templates ────────────────────────────────────────────────

    private async Task<Result<ItemTemplateSnapshot>> GetItemTemplateByKeyCoreAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<ItemTemplateSnapshot>.Failure(Error.Create(
                "catalog.item_template_key_required",
                "Item template key is required."));
        }

        var encodedKey = Uri.EscapeDataString(key.Trim());
        var url = $"/api/v2/catalog/items/{encodedKey}";
        var wrapper = await GetJsonOrNullAsync<GetItemTemplateByKeyHttpResponse>(url, cancellationToken);

        if (wrapper?.Template is null)
        {
            return Result<ItemTemplateSnapshot>.Failure(Error.Create(
                "catalog.item_template_not_found",
                $"Item template '{key}' was not found."));
        }

        return Result<ItemTemplateSnapshot>.Success(
            MapToItemTemplateSnapshot(wrapper.Template));
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

    public async Task<IReadOnlyCollection<CatalogSkillDefinition>> ListActiveSkillDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/skill-definitions";

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

    public async Task<IReadOnlyCollection<CatalogItemDefinitionSnapshot>> ListActiveItemDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/item-definitions";

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

        ListItemDefinitionsHttpResponse? wrapper;
        try
        {
            wrapper = await response.Content
                .ReadFromJsonAsync<ListItemDefinitionsHttpResponse>(options, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CatalogGatewayException(
                $"Failed to deserialize Catalog Service response from '{url}': {ex.Message}", ex);
        }

        return wrapper?.Definitions?
            .Select(MapToCatalogItemDefinitionSnapshot)
            .ToArray() ?? [];
    }

    private static CatalogSkillDefinition MapToCatalogSkillDefinition(
        CatalogSkillDefinitionHttpResponse source)
    {
        return new CatalogSkillDefinition(
            Key: source.Key, DisplayName: source.Name, Description: source.Description,
            SkillType: source.SkillType, TargetingType: source.TargetingType, EffectType: source.EffectType,
            ManaCost: source.ManaCost, ChargeCost: source.ChargeCost, BasePower: source.BasePower,
            Tags: source.Tags ?? [],
            Effects: source.Effects?
                .Select(e => new CatalogSkillEffectSpec(
                    e.Kind, e.StatusKey, e.Magnitude, e.DurationTicks, e.TickInterval, e.Stat,
                    e.MagnitudeIsPercentOfMax, e.MagnitudeIsPercentOfBaseStat, e.AppliesToActor,
                    e.IsPermanent))
                .ToArray(),
            Category: source.Category,
            BasePowerIsPercentOfMaxVitality: source.BasePowerIsPercentOfMaxVitality,
            TacticalRange: source.TacticalRange,
            TacticalAreaShape: source.TacticalAreaShape,
            RequiresLineOfSight: source.RequiresLineOfSight,
            Cooldown: source.Cooldown,
            IsUltimate: source.IsUltimate,
            EmotionalRegister: source.EmotionalRegister);
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
            SkillKeys: source.SkillKeys,
            AttackPower: source.AttackPower,
            Defense: source.Defense,
            Speed: source.Speed,
            Focus: source.Focus,
            Initiative: source.Initiative,
            Mana: source.Mana,
            MagicAttack: source.MagicAttack,
            MagicDefense: source.MagicDefense,
            Menace: source.Menace,
            Rarity: source.Rarity,
            Registre: source.Registre,
            BoundRoomKeys: source.BoundRoomKeys,
            Movement: source.Movement);
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
            Effects: source.Effects?.Select(MapToCatalogEffectDefinitionSnapshot).ToArray() ?? [],
            BaseWeight: source.BaseWeight,
            MinDepth: source.MinDepth,
            MaxDepth: source.MaxDepth,
            Rarity: source.Rarity,
            Polarity: source.Polarity,
            IsMajeure: source.IsMajeure,
            RoomKey: source.RoomKey,
            IsCumulExempt: source.IsCumulExempt,
            ExclusionKeys: source.ExclusionKeys ?? []);
    }

    private static CatalogCurseDefinitionSnapshot MapToCatalogCurseDefinitionSnapshot(
        CatalogCurseDefinitionHttpResponse source)
    {
        return new CatalogCurseDefinitionSnapshot(
            source.Key,
            source.Version,
            source.DisplayName,
            source.Description,
            source.NarrativeText,
            source.Severity,
            source.Duration,
            source.Trigger,
            source.EffectSetKey);
    }

    private static CatalogItemDefinitionSnapshot MapToCatalogItemDefinitionSnapshot(
        CatalogItemDefinitionHttpResponse source)
    {
        return new CatalogItemDefinitionSnapshot(
            source.Key,
            source.Version,
            source.DisplayName,
            source.Description,
            source.NarrativeText,
            source.Category,
            source.ItemType,
            source.Rarity,
            source.UsageMode,
            source.Lifecycle,
            source.StackPolicy,
            source.MaxStack,
            source.IsUsableInCombat,
            source.IsUsableOutsideCombat,
            source.EffectSetKey,
            source.IsPermanentEligible,
            source.EquipmentEffects?
                .Select(e => new CatalogItemEquipmentEffect(e.Kind, e.StatKind, e.Amount, e.SkillKey, e.AffinityRegister))
                .ToArray(),
            source.IsContainer,
            source.ContainerCapacity,
            source.IsLiquid,
            source.EffectValue,
            source.EffectRunType,
            source.ReadablePages,
            source.TacticalRange,
            source.TacticalAreaShape,
            source.RequiresLineOfSight,
            source.BasicAttackPower,
            source.BasicAttackCategory,
            source.PalaceShardCost,
            source.HimLitShardCost);
    }

    private static CatalogEffectSetSnapshot MapToCatalogEffectSetSnapshot(
        CatalogEffectSetHttpResponse source)
    {
        return new CatalogEffectSetSnapshot(
            source.Key,
            source.Version,
            source.Effects?.Select(MapToCatalogEffectDefinitionSnapshot).ToArray() ?? []);
    }

    private static CatalogRewardTemplateSnapshot MapToCatalogRewardTemplateSnapshot(
        CatalogRewardTemplateHttpResponse source)
    {
        return new CatalogRewardTemplateSnapshot(
            source.Key,
            source.Version,
            source.DisplayName,
            source.Description,
            source.SourceType,
            source.MinChoices,
            source.MaxChoices,
            source.Options?.Select(MapToCatalogRewardTemplateOptionSnapshot).ToArray() ?? []);
    }

    private static CatalogRewardTemplateOptionSnapshot MapToCatalogRewardTemplateOptionSnapshot(
        CatalogRewardTemplateOptionHttpResponse source)
    {
        return new CatalogRewardTemplateOptionSnapshot(
            source.RewardType,
            source.Label,
            source.Description,
            source.PayloadKey,
            source.PayloadType,
            source.EffectSetKey,
            source.BaseAmount,
            source.ScalingMode,
            source.Weight,
            source.ItemType,
            source.ItemRarity,
            source.ItemEffectType);
    }

    private static ItemTemplateSnapshot MapToItemTemplateSnapshot(
        ItemTemplateHttpResponse source)
    {
        return new ItemTemplateSnapshot(
            Key: source.Key,
            Name: source.Name,
            Description: source.Description,
            Version: source.Version,
            Status: source.Status,
            ItemType: source.Category,
            Rarity: source.Rarity,
            IsTemporary: false,
            EffectTags: []);
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

    public async Task<IReadOnlyCollection<CatalogRoomDefinition>> ListRoomDefinitionsAsync(
    CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/room-definitions";
        var wrapper = await GetJsonOrNullAsync<ListRoomDefinitionsHttpResponse>(url, cancellationToken);
        return wrapper?.Definitions?.Select(MapToCatalogRoomDefinition).ToArray() ?? [];
    }

    private static CatalogRoomDefinition MapToCatalogRoomDefinition(CatalogRoomDefinitionHttpResponse source)
        => new(
            source.Key, source.DisplayName, source.Description, source.NarrativeText,
            source.RoomFamily, source.RoomRarity, source.Theme,
            source.MinDepth ?? 0, source.MaxDepth ?? int.MaxValue, source.BaseWeight,
            source.EnemyPoolKey, source.RewardPoolKey, source.LawPoolKey, source.CursePoolKey,
            source.BossDefinitionKey, source.IsUnique,
            source.WorldKey, source.IsWorldEntryRoom, source.TriggersStrictChain,
            source.ReachableRoomKeys ?? []);

    private sealed record ListRoomDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogRoomDefinitionHttpResponse>? Definitions);

    private sealed record CatalogRoomDefinitionHttpResponse(
        string Key, string DisplayName, string Description, string? NarrativeText,
        string RoomFamily, string RoomRarity, string Theme,
        int? MinDepth, int? MaxDepth, int BaseWeight,
        string? EnemyPoolKey, string? RewardPoolKey, string? LawPoolKey, string? CursePoolKey,
        string? BossDefinitionKey, bool IsUnique,
        string? WorldKey, bool IsWorldEntryRoom, bool TriggersStrictChain,
        IReadOnlyCollection<string>? ReachableRoomKeys);

    public async Task<IReadOnlyCollection<CatalogWorldDefinition>> ListWorldDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/worlds";
        var wrapper = await GetJsonOrNullAsync<ListWorldDefinitionsHttpResponse>(url, cancellationToken);
        return wrapper?.Definitions?
            .Select(d => new CatalogWorldDefinition(d.Key, d.DisplayName, d.EntryRoomKey))
            .ToArray() ?? [];
    }

    private sealed record ListWorldDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogWorldDefinitionHttpResponse>? Definitions);

    private sealed record CatalogWorldDefinitionHttpResponse(
        Guid Id, string Key, string DisplayName, string EntryRoomKey, string Version, string Status);

    public async Task<IReadOnlyCollection<CatalogRoomThemeAffinity>> ListRoomThemeAffinitiesAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/room-theme-affinities";
        var wrapper = await GetJsonOrNullAsync<ListRoomThemeAffinitiesHttpResponse>(url, cancellationToken);
        return wrapper?.Affinities?
            .Select(a => new CatalogRoomThemeAffinity(a.ThemeFrom, a.ThemeTo, a.Weight))
            .ToArray() ?? [];
    }

    private sealed record ListRoomThemeAffinitiesHttpResponse(
        IReadOnlyCollection<CatalogRoomThemeAffinityHttpResponse>? Affinities);

    private sealed record CatalogRoomThemeAffinityHttpResponse(
        Guid Id, string ThemeFrom, string ThemeTo, decimal Weight);

    public async Task<IReadOnlyCollection<CatalogNpcReputationAffinity>> ListNpcReputationAffinitiesAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/npc-reputation-affinities";
        var wrapper = await GetJsonOrNullAsync<ListNpcReputationAffinitiesHttpResponse>(url, cancellationToken);
        return wrapper?.Affinities?
            .Select(a => new CatalogNpcReputationAffinity(a.NpcKeyFrom, a.NpcKeyTo, a.Weight))
            .ToArray() ?? [];
    }

    private sealed record ListNpcReputationAffinitiesHttpResponse(
        IReadOnlyCollection<CatalogNpcReputationAffinityHttpResponse>? Affinities);

    private sealed record CatalogNpcReputationAffinityHttpResponse(
        Guid Id, string NpcKeyFrom, string NpcKeyTo, decimal Weight);

    public async Task<IReadOnlyCollection<CatalogRoomTypeDefinition>> ListRoomTypeDefinitionsAsync(
    CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/room-type-definitions";

        var wrapper = await GetJsonOrNullAsync<ListRoomTypeDefinitionsHttpResponse>(url, cancellationToken);

        return wrapper?.Definitions?
            .Select(d => new CatalogRoomTypeDefinition(
                d.Key,
                d.DisplayName,
                d.Theme,
                d.MinDepth ?? 0,
                d.MaxDepth ?? int.MaxValue))
            .ToArray()
            ?? [];
    }
    private sealed record ListRoomTypeDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogRoomTypeDefinitionHttpResponse>? Definitions);

    private sealed record CatalogRoomTypeDefinitionHttpResponse(
        string Key,
        string DisplayName,
        string Theme,
        int? MinDepth,
        int? MaxDepth);

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
            MaxDepth: source.MaxDepth ?? int.MaxValue,
            EmotionalAffinity: source.EmotionalAffinity ?? "Neutral",
            IsRecurring: source.IsRecurring,
            Persona: source.Persona is null ? null : MapNpcPersona(source.Persona),
            DialogueGraph: source.DialogueGraph is null ? null : MapNpcDialogueGraph(source.DialogueGraph),
            Wounds: (source.Wounds ?? []).Select(MapNpcWound).ToArray(),
            EncounterKeys: source.EncounterKeys ?? [],
            BoundRoomKeys: source.BoundRoomKeys ?? [],
            Offerings: (source.Offerings ?? []).Select(MapNpcOffering).ToArray());
    }

    private static CatalogNpcOffering MapNpcOffering(CatalogNpcOfferingHttpResponse s) =>
        new(s.Key, s.Kind, s.TargetKey, s.Amount, s.IsMajor,
            (s.UnlockConditions ?? []).Select(MapDialogueRequirement).ToArray(),
            s.CompanionKit is null ? null : MapCompanionKit(s.CompanionKit));

    private static CatalogCompanionKit MapCompanionKit(CatalogCompanionKitHttpResponse s) =>
        new(s.MaxVitality, s.AttackPower, s.Defense, s.StartingGuard, s.Speed,
            s.Initiative, s.Focus, s.Mana, s.Charge, s.SkillKeys ?? [],
            s.MagicAttack, s.MagicDefense);

    private static CatalogNpcPersona MapNpcPersona(CatalogNpcPersonaHttpResponse s) =>
        new(s.Tone, s.Register, s.Needs ?? [], s.Offerings ?? []);

    private static CatalogNpcTransgression MapNpcTransgression(CatalogNpcTransgressionHttpResponse s) =>
        new(s.WoundKey, s.TriggerFlag, s.RelationshipPenalty);

    private static CatalogNpcWound MapNpcWound(CatalogNpcWoundHttpResponse s) =>
        new(s.Key, s.WoundRegister, s.Reversibility, s.TenseThreshold, s.RuptureThreshold,
            (s.Transgressions ?? []).Select(MapNpcTransgression).ToArray(), s.RupturedNarrativeKey);

    private static CatalogDialogueRequirement MapDialogueRequirement(CatalogDialogueRequirementHttpResponse s) =>
        new(s.Kind, s.FlagKey, s.WoundKey, s.RequiredWoundState, s.RequiredRelationshipScore);

    private static CatalogDialogueConsequence MapDialogueConsequence(CatalogDialogueConsequenceHttpResponse s) =>
        new(s.Kind, s.WhenWoundState, s.NarrativeFragmentKey, s.RewardCursePoolKey, s.EncounterKey,
            s.RelationshipDelta, s.MemoryFlag, s.WoundKey, s.OfferingKey,
            s.OnWin?.Select(MapDialogueConsequence).ToArray(),
            s.OnFlee?.Select(MapDialogueConsequence).ToArray(),
            s.OnLose?.Select(MapDialogueConsequence).ToArray());

    private static CatalogNpcDialogueChoice MapDialogueChoice(CatalogNpcDialogueChoiceHttpResponse s) =>
        new(s.Key, s.Label,
            (s.Requirements ?? []).Select(MapDialogueRequirement).ToArray(),
            (s.Consequences ?? []).Select(MapDialogueConsequence).ToArray(),
            s.NextNodeKey);

    private static CatalogNpcDialogueNode MapDialogueNode(CatalogNpcDialogueNodeHttpResponse s) =>
        new(s.Key, s.Speaker, s.Lines ?? [], (s.Choices ?? []).Select(MapDialogueChoice).ToArray(),
            s.TenseLines, s.RupturedLines);

    private static CatalogNpcDialogueGraph MapNpcDialogueGraph(CatalogNpcDialogueGraphHttpResponse s) =>
        new(s.Key, s.Version, s.EntryNodeKey,
            (s.Nodes ?? new Dictionary<string, CatalogNpcDialogueNodeHttpResponse>())
                .ToDictionary(kv => kv.Key, kv => MapDialogueNode(kv.Value)));

    public async Task<IReadOnlyCollection<CatalogRewardCursePool>> ListRewardCursePoolsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/reward-curse-pools";

        var wrapper = await GetJsonOrNullAsync<ListRewardCursePoolsHttpResponse>(url, cancellationToken);

        return wrapper?.Pools?
            .Select(MapRewardCursePool)
            .ToArray()
            ?? [];
    }

    public async Task<CatalogEnemyLootTable?> GetEnemyLootTableByKeyAsync(
        string enemyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(enemyKey))
        {
            return null;
        }

        var encodedKey = Uri.EscapeDataString(enemyKey.Trim());
        var url = $"/api/v2/catalog/enemy-loot-tables/{encodedKey}";

        var wrapper = await GetJsonOrNullAsync<GetEnemyLootTableByKeyHttpResponse>(url, cancellationToken);

        return wrapper?.LootTable is null ? null : MapEnemyLootTable(wrapper.LootTable);
    }

    public async Task<CatalogGenericLootPool?> GetActiveGenericLootPoolAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/generic-loot-pool";

        var wrapper = await GetJsonOrNullAsync<GetActiveGenericLootPoolHttpResponse>(url, cancellationToken);

        return wrapper?.Pool is null ? null : MapGenericLootPool(wrapper.Pool);
    }

    private static CatalogEnemyLootTable MapEnemyLootTable(CatalogEnemyLootTableHttpResponse s) =>
        new(s.Key, s.EnemyDefinitionKey, s.Name, s.Description, s.Version,
            (s.Entries ?? []).Select(MapLootEntry).ToArray());

    private static CatalogGenericLootPool MapGenericLootPool(CatalogGenericLootPoolHttpResponse s) =>
        new(s.Key, s.Name, s.Description, s.Version,
            (s.Entries ?? []).Select(MapLootEntry).ToArray());

    private static CatalogLootEntry MapLootEntry(CatalogLootEntryHttpResponse s) =>
        new(s.ItemDefinitionKey, s.DropPercent);

    private static CatalogRewardCursePool MapRewardCursePool(CatalogRewardCursePoolHttpResponse s) =>
        new(s.Key, s.Name, s.Description, s.Version,
            (s.Entries ?? []).Select(MapRewardCurseEntry).ToArray());

    private static CatalogRewardCurseEntry MapRewardCurseEntry(CatalogRewardCurseEntryHttpResponse s) =>
        new(s.Kind, s.ResultKind, s.TargetKey, s.Amount,
            (s.Availability ?? []).Select(MapRewardCurseAvailability).ToArray());

    private static CatalogRewardCurseAvailability MapRewardCurseAvailability(CatalogRewardCurseAvailabilityHttpResponse s) =>
        new(s.Kind, s.Value);

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

    private sealed record GetCurseDefinitionByKeyHttpResponse(
        CatalogCurseDefinitionHttpResponse? Definition);

    private sealed record ListCurseDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogCurseDefinitionHttpResponse>? Definitions);

    private sealed record CatalogCurseDefinitionHttpResponse(
        string Key,
        string Version,
        string DisplayName,
        string Description,
        string? NarrativeText,
        int Severity,
        string Duration,
        string? Trigger,
        string? EffectSetKey);

    private sealed record GetItemDefinitionByKeyHttpResponse(
        CatalogItemDefinitionHttpResponse? Definition);

    private sealed record ListItemDefinitionsHttpResponse(
        IReadOnlyCollection<CatalogItemDefinitionHttpResponse>? Definitions);

    private sealed record CatalogItemDefinitionHttpResponse(
        string Key,
        string Version,
        string DisplayName,
        string Description,
        string? NarrativeText,
        string Category,
        string ItemType,
        string Rarity,
        string UsageMode,
        string Lifecycle,
        string StackPolicy,
        int MaxStack,
        bool IsUsableInCombat,
        bool IsUsableOutsideCombat,
        string? EffectSetKey,
        bool IsPermanentEligible = false,
        IReadOnlyCollection<CatalogItemEquipmentEffectHttpResponse>? EquipmentEffects = null,
        bool IsContainer = false,
        int? ContainerCapacity = null,
        bool IsLiquid = false,
        int EffectValue = 0,
        string? EffectRunType = null,
        IReadOnlyCollection<string>? ReadablePages = null,
        int TacticalRange = 1,
        string TacticalAreaShape = "Single",
        bool RequiresLineOfSight = false,
        int? BasicAttackPower = null,
        string? BasicAttackCategory = null,
        int PalaceShardCost = 0,
        int HimLitShardCost = 0);

    private sealed record CatalogItemEquipmentEffectHttpResponse(
        string Kind,
        string? StatKind,
        int? Amount,
        string? SkillKey,
        string? AffinityRegister);

    private sealed record GetEffectSetByKeyHttpResponse(
        CatalogEffectSetHttpResponse? Definition);

    private sealed record CatalogEffectSetHttpResponse(
        string Key,
        string Version,
        string Status,
        IReadOnlyCollection<CatalogEffectDefinitionHttpResponse>? Effects);

    private sealed record GetRewardTemplateByKeyHttpResponse(
        CatalogRewardTemplateHttpResponse? Definition);

    private sealed record ListRewardTemplatesHttpResponse(
        IReadOnlyCollection<CatalogRewardTemplateHttpResponse>? Definitions);

    private sealed record CatalogRewardTemplateHttpResponse(
        string Key,
        string Version,
        string DisplayName,
        string Description,
        string SourceType,
        int MinChoices,
        int MaxChoices,
        string Status,
        IReadOnlyCollection<CatalogRewardTemplateOptionHttpResponse>? Options);

    private sealed record CatalogRewardTemplateOptionHttpResponse(
        string RewardType,
        string Label,
        string Description,
        string? PayloadKey,
        string? PayloadType,
        string? EffectSetKey,
        int BaseAmount,
        string ScalingMode,
        int Weight,
        string? ItemType = null,
        string? ItemRarity = null,
        string? ItemEffectType = null);

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
        IReadOnlyCollection<CatalogEffectDefinitionHttpResponse>? Effects,
        int BaseWeight = 1,
        int? MinDepth = null,
        int? MaxDepth = null,
        string Rarity = "Commun",
        string Polarity = "Neutre",
        bool IsMajeure = false,
        string? RoomKey = null,
        bool IsCumulExempt = false,
        IReadOnlyCollection<string>? ExclusionKeys = null);

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

    private sealed record ListRewardCursePoolsHttpResponse(
    IReadOnlyCollection<CatalogRewardCursePoolHttpResponse>? Pools);

    private sealed record GetEnemyLootTableByKeyHttpResponse(
        CatalogEnemyLootTableHttpResponse? LootTable);

    private sealed record CatalogEnemyLootTableHttpResponse(
        string Key,
        string EnemyDefinitionKey,
        string Name,
        string Description,
        string Version,
        string Status,
        IReadOnlyCollection<CatalogLootEntryHttpResponse>? Entries);

    private sealed record GetActiveGenericLootPoolHttpResponse(
        CatalogGenericLootPoolHttpResponse? Pool);

    private sealed record CatalogGenericLootPoolHttpResponse(
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        IReadOnlyCollection<CatalogLootEntryHttpResponse>? Entries);

    private sealed record CatalogLootEntryHttpResponse(
        string ItemDefinitionKey,
        int DropPercent);

    private sealed record CatalogRewardCursePoolHttpResponse(
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        IReadOnlyCollection<CatalogRewardCurseEntryHttpResponse>? Entries);

    private sealed record CatalogRewardCurseEntryHttpResponse(
        string Kind,
        string ResultKind,
        string? TargetKey,
        int Amount,
        IReadOnlyCollection<CatalogRewardCurseAvailabilityHttpResponse>? Availability);

    private sealed record CatalogRewardCurseAvailabilityHttpResponse(
        string Kind,
        int Value);

    private sealed record CatalogNpcDefinitionHttpResponse(
        string Key,
        string Name,
        string Description,
        IReadOnlyCollection<string>? Tags,
        IReadOnlyCollection<string>? CompatibleRoomTypes,
        IReadOnlyCollection<string>? CompatiblePalaceRoomStates,
        IReadOnlyCollection<string>? CompatibleRoomClimates,
        int? MinDepth,
        int? MaxDepth,
        string? EmotionalAffinity,
        bool IsRecurring,
        CatalogNpcPersonaHttpResponse? Persona,
        CatalogNpcDialogueGraphHttpResponse? DialogueGraph,
        IReadOnlyCollection<CatalogNpcWoundHttpResponse>? Wounds,
        IReadOnlyCollection<string>? EncounterKeys,
        IReadOnlyCollection<string>? BoundRoomKeys,
        IReadOnlyCollection<CatalogNpcOfferingHttpResponse>? Offerings);

    private sealed record CatalogNpcOfferingHttpResponse(
        string Key,
        string Kind,
        string? TargetKey,
        int Amount,
        bool IsMajor,
        IReadOnlyCollection<CatalogDialogueRequirementHttpResponse>? UnlockConditions,
        CatalogCompanionKitHttpResponse? CompanionKit = null);

    private sealed record CatalogCompanionKitHttpResponse(
        int MaxVitality,
        int AttackPower,
        int Defense,
        int StartingGuard,
        int Speed,
        int Initiative,
        int Focus,
        int Mana,
        int Charge,
        IReadOnlyCollection<string>? SkillKeys,
        int MagicAttack = 0,
        int MagicDefense = 0);

    private sealed record CatalogNpcPersonaHttpResponse(
        string Tone,
        string Register,
        IReadOnlyCollection<string>? Needs,
        IReadOnlyCollection<string>? Offerings);

    private sealed record CatalogNpcTransgressionHttpResponse(
        string WoundKey,
        string TriggerFlag,
        int RelationshipPenalty);

    private sealed record CatalogNpcWoundHttpResponse(
        string Key,
        string WoundRegister,
        string Reversibility,
        int TenseThreshold,
        int RuptureThreshold,
        IReadOnlyCollection<CatalogNpcTransgressionHttpResponse>? Transgressions,
        string? RupturedNarrativeKey);

    private sealed record CatalogDialogueRequirementHttpResponse(
        string Kind,
        string? FlagKey,
        string? WoundKey,
        string? RequiredWoundState,
        int? RequiredRelationshipScore = null);

    private sealed record CatalogDialogueConsequenceHttpResponse(
        string Kind,
        string? WhenWoundState,
        string? NarrativeFragmentKey,
        string? RewardCursePoolKey,
        string? EncounterKey,
        int RelationshipDelta,
        string? MemoryFlag,
        string? WoundKey,
        string? OfferingKey,
        IReadOnlyCollection<CatalogDialogueConsequenceHttpResponse>? OnWin,
        IReadOnlyCollection<CatalogDialogueConsequenceHttpResponse>? OnFlee,
        IReadOnlyCollection<CatalogDialogueConsequenceHttpResponse>? OnLose);

    private sealed record CatalogNpcDialogueChoiceHttpResponse(
        string Key,
        string Label,
        IReadOnlyCollection<CatalogDialogueRequirementHttpResponse>? Requirements,
        IReadOnlyCollection<CatalogDialogueConsequenceHttpResponse>? Consequences,
        string? NextNodeKey);

    private sealed record CatalogNpcDialogueNodeHttpResponse(
        string Key,
        string Speaker,
        IReadOnlyCollection<string>? Lines,
        IReadOnlyCollection<CatalogNpcDialogueChoiceHttpResponse>? Choices,
        IReadOnlyCollection<string>? TenseLines,
        IReadOnlyCollection<string>? RupturedLines);

    private sealed record CatalogNpcDialogueGraphHttpResponse(
        string Key,
        string Version,
        string EntryNodeKey,
        IReadOnlyDictionary<string, CatalogNpcDialogueNodeHttpResponse>? Nodes);

    // ── Template HTTP responses ───────────────────────────────────────

    private sealed record GetItemTemplateByKeyHttpResponse(
        ItemTemplateHttpResponse? Template);

    private sealed record ItemTemplateHttpResponse(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Category,
        string Rarity,
        string Duration,
        int EffectValue,
        int Price);

}
