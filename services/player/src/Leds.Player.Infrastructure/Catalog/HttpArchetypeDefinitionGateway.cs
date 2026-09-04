using System.Net;
using System.Net.Http.Json;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;

namespace Leds.Player.Infrastructure.Catalog;

public sealed class HttpArchetypeDefinitionGateway : IArchetypeDefinitionGateway
{
    private readonly HttpClient _client;
    public HttpArchetypeDefinitionGateway(HttpClient client) => _client = client;

    public async Task<ArchetypeDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync(
            $"/api/v2/catalog/archetype-definitions/{Uri.EscapeDataString(key)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Response>(cancellationToken);
        var definition = body?.Definition
            ?? throw new InvalidOperationException("Catalog returned an empty archetype definition.");
        var stats = definition.BaseStats;
        return new ArchetypeDefinitionSnapshot(
            definition.Key,
            PlayerCharacterStatBlock.Create(
                stats.MaxVitality, stats.AttackPower, stats.Defense, stats.StartingGuard,
                stats.Speed, stats.Initiative, stats.Focus, stats.Mana, stats.Charge,
                stats.MagicAttack, stats.MagicDefense, stats.Movement),
            definition.ProficiencyTags ?? [],
            (definition.StarterEquipment ?? []).Select(item => new ArchetypeStarterEquipment(
                item.ItemDefinitionKey,
                Enum.TryParse<EquipmentPosition>(item.EquipmentPosition, true, out var position)
                    ? position
                    : throw new InvalidOperationException(
                        $"Catalog returned unknown equipment position '{item.EquipmentPosition}'."))).ToArray(),
            definition.StarterKnownSkills ?? [],
            definition.StarterEquippedSkills ?? []);
    }

    private sealed record Response(Definition? Definition);
    private sealed record Definition(
        string Key,
        BaseStats BaseStats,
        IReadOnlyCollection<string>? ProficiencyTags,
        IReadOnlyCollection<StarterEquipment>? StarterEquipment,
        IReadOnlyCollection<string>? StarterKnownSkills,
        IReadOnlyCollection<string>? StarterEquippedSkills);
    private sealed record BaseStats(
        int MaxVitality, int AttackPower, int MagicAttack, int Defense, int MagicDefense,
        int StartingGuard, int Speed, int Initiative, int Focus, int Mana, int Charge, int Movement);
    private sealed record StarterEquipment(string ItemDefinitionKey, string EquipmentPosition);
}
