using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Combats.Typing;
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

    private static string Require(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Catalog returned an empty {field}.")
            : value.Trim();

    public async Task<CatalogItemTypeCatalog> GetItemTypeCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/item-types";
        var response = await GetJsonOrNullAsync<ItemTypeCatalogHttpResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Catalog returned no item type catalog.");

        if (string.IsNullOrWhiteSpace(response.Version)
            || response.Definitions is null
            || response.Definitions.Count == 0)
        {
            throw new InvalidOperationException("Catalog returned an incomplete item type catalog.");
        }

        var definitions = response.Definitions.Select(definition => new CatalogItemTypeDefinition(
            Require(definition.Code, "item type code").ToLowerInvariant(),
            Require(definition.DisplayName, $"item type '{definition.Code}' display name"),
            Require(definition.Glyph, $"item type '{definition.Code}' glyph"),
            Require(definition.Color, $"item type '{definition.Code}' color"))).ToArray();

        if (definitions.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != definitions.Length)
        {
            throw new InvalidOperationException("Catalog returned duplicate item type definitions.");
        }

        return new CatalogItemTypeCatalog(response.Version.Trim(), definitions);
    }

    public async Task<CatalogItemRarityCatalog> GetItemRarityCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/item-rarities";
        var response = await GetJsonOrNullAsync<ItemRarityCatalogHttpResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Catalog returned no item rarity catalog.");

        if (string.IsNullOrWhiteSpace(response.Version)
            || response.Definitions is null
            || response.Definitions.Count == 0)
        {
            throw new InvalidOperationException("Catalog returned an incomplete item rarity catalog.");
        }

        var definitions = response.Definitions.Select(definition => new CatalogItemRarityDefinition(
            Require(definition.Code, "item rarity code").ToLowerInvariant(),
            Require(definition.DisplayName, $"item rarity '{definition.Code}' display name"),
            Require(definition.Glyph, $"item rarity '{definition.Code}' glyph"),
            Require(definition.Color, $"item rarity '{definition.Code}' color"),
            definition.PalaceShardCost,
            definition.HimLitShardCost)).ToArray();

        if (definitions.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != definitions.Length)
        {
            throw new InvalidOperationException("Catalog returned duplicate item rarity definitions.");
        }

        return new CatalogItemRarityCatalog(response.Version.Trim(), definitions);
    }

    public async Task<CatalogEmotionalRegisterCatalog> GetEmotionalRegisterCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/emotional-registers";
        var response = await GetJsonOrNullAsync<EmotionalRegisterCatalogHttpResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Catalog returned no emotional register catalog.");

        if (string.IsNullOrWhiteSpace(response.Version)
            || response.Definitions is null
            || response.Definitions.Count == 0)
        {
            throw new InvalidOperationException("Catalog returned an incomplete emotional register catalog.");
        }

        var definitions = response.Definitions.Select(definition =>
        {
            _ = EmotionalTypeCode.ParseRequired(
                definition.Code,
                $"Emotional register '{definition.Code}' code");

            if (string.IsNullOrWhiteSpace(definition.DisplayName)
                || string.IsNullOrWhiteSpace(definition.Glyph)
                || string.IsNullOrWhiteSpace(definition.Color))
            {
                throw new InvalidOperationException(
                    $"Catalog returned incomplete metadata for emotional register '{definition.Code}'.");
            }

            return new CatalogEmotionalRegisterDefinition(
                definition.Code.Trim().ToLowerInvariant(),
                definition.DisplayName.Trim(),
                definition.Glyph,
                definition.Color.Trim(),
                definition.IncomingAffinities?.Select(affinity => new CatalogBaseEmotionalAffinity(
                    EmotionalTypeCode.ParseRequired(
                        affinity.IncomingRegister,
                        $"Register '{definition.Code}' incoming affinity").ToString().ToLowerInvariant(),
                    Enum.TryParse<DamageEffectiveness>(affinity.Outcome, true, out var outcome)
                        ? outcome.ToString()
                        : throw new InvalidOperationException(
                            $"Catalog returned unknown affinity outcome '{affinity.Outcome}'."),
                    affinity.Multiplier)).ToArray()
                    ?? throw new InvalidOperationException(
                        $"Catalog returned no affinities for emotional register '{definition.Code}'."));
        }).ToArray();

        if (definitions.Select(definition => definition.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != definitions.Length)
        {
            throw new InvalidOperationException("Catalog returned duplicate emotional register definitions.");
        }

        var codes = definitions.Select(definition => definition.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            var affinities = definition.IncomingAffinities;
            if (affinities.Count != definitions.Length
                || affinities.Select(affinity => affinity.IncomingRegister)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(codes) is false
                || affinities.Any(affinity => !double.IsFinite(affinity.Multiplier) || affinity.Multiplier < 0))
            {
                throw new InvalidOperationException(
                    $"Catalog returned an incomplete affinity profile for emotional register '{definition.Code}'.");
            }
        }

        return new CatalogEmotionalRegisterCatalog(response.Version.Trim(), definitions);
    }

    public async Task<CatalogEmotionalAffinityMatrixSnapshot> GetEmotionalAffinityMatrixAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/emotional-affinity-matrix";
        var response = await GetJsonOrNullAsync<EmotionalAffinityMatrixHttpResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Catalog returned no emotional affinity matrix.");

        if (string.IsNullOrWhiteSpace(response.Version) || response.Rules is null)
            throw new InvalidOperationException("Catalog returned an incomplete emotional affinity matrix.");

        return new CatalogEmotionalAffinityMatrixSnapshot(
            response.Version,
            response.Rules.Select(rule => new CatalogEmotionalAffinityRuleSnapshot(
                rule.AttackingRegister,
                rule.DefendingRegister,
                rule.Outcome,
                rule.Multiplier)).ToArray());
    }

    public async Task<IReadOnlyCollection<CatalogCharacterCombatDefinition>> ListCharacterCombatDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "/api/v2/catalog/character-combat-definitions";
        var response = await GetJsonOrNullAsync<CharacterCombatDefinitionsHttpResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Catalog returned no character combat definitions.");

        var definitions = response.Definitions?.Select(definition => new CatalogCharacterCombatDefinition(
            Require(definition.DefinitionKey, "Character definition key"),
            Require(definition.Kind, $"Character '{definition.DefinitionKey}' kind"),
            Require(definition.CombatArchetypeCode, $"Character '{definition.DefinitionKey}' archetype"),
            EmotionalTypeCode.ParseRequired(
                definition.EmotionalRegister,
                $"Character '{definition.DefinitionKey}' emotional register").ToString().ToLowerInvariant()))
            .ToArray()
            ?? throw new InvalidOperationException("Catalog returned no character combat definitions.");

        if (definitions.Length == 0
            || definitions.Select(definition => definition.DefinitionKey)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != definitions.Length)
        {
            throw new InvalidOperationException("Catalog returned empty or duplicate character combat definitions.");
        }

        return definitions;
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
        CancellationToken c