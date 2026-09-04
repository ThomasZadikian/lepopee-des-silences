using System.Net;
using System.Net.Http.Json;
using Leds.Player.Application.Abstractions;

namespace Leds.Player.Infrastructure.Catalog;

public sealed class HttpEquipmentDefinitionGateway : IEquipmentDefinitionGateway
{
    private readonly HttpClient _client;
    public HttpEquipmentDefinitionGateway(HttpClient client) => _client = client;

    public async Task<EquipmentDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync(
            $"/api/v2/catalog/item-definitions/{Uri.EscapeDataString(key)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var dto = (await response.Content.ReadFromJsonAsync<Response>(cancellationToken))?.Definition
            ?? throw new InvalidOperationException("Catalog returned an empty item definition.");
        return new EquipmentDefinitionSnapshot(
            dto.Key, dto.DisplayName, dto.AllowedSlots ?? [], dto.UniqueEquipGroup,
            dto.ProficiencyTags ?? [],
            (dto.EquipmentEffects ?? []).Select(effect => new EquipmentEffectSnapshot(
                effect.Kind, effect.StatKind, effect.Amount, effect.SkillKey)).ToArray());
    }

    private sealed record Response(Definition? Definition);
    private sealed record Definition(
        string Key, string DisplayName, IReadOnlyCollection<string>? AllowedSlots,
        string? UniqueEquipGroup, IReadOnlyCollection<string>? ProficiencyTags,
        IReadOnlyCollection<Effect>? EquipmentEffects);
    private sealed record Effect(string Kind, string? StatKind, int? Amount, string? SkillKey);
}
