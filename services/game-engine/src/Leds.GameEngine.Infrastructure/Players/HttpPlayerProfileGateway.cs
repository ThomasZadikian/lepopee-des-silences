using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Common;
using System.Net.Http.Json;

namespace Leds.GameEngine.Infrastructure.Players;

public sealed class HttpPlayerProfileGateway : IPlayerProfileGateway
{
    private readonly HttpClient _httpClient;

    public HttpPlayerProfileGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task AwardStatPointAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/internal/players/{playerId}/stat-points/award", content: null, cancellationToken);

        EnsureSuccess(response, playerId);
    }

    public async Task<PlayerProfileView> GetProfileAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/api/v2/players/{playerId}", cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> EquipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/skills/{skillKey}/equip", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> UnequipSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/skills/{skillKey}/unequip", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/stats/{stat}/spend-point", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    private static void EnsureSuccess(HttpResponseMessage response, Guid playerId)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Player", playerId);
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<PlayerProfileView> ReadProfileAsync(HttpResponseMessage response, Guid playerId, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Player", playerId);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var conflictBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException(conflictBody);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var badRequestBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new DomainException(badRequestBody);
        }

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PlayerProfileResponse>(cancellationToken);

        if (dto is null)
        {
            throw new InvalidOperationException("Player Service returned an empty profile.");
        }

        return new PlayerProfileView(
            Id: dto.Id,
            DisplayName: dto.DisplayName,
            Characters: dto.Characters
                .Select(c => new PlayerCharacterView(
                    Id: c.Id,
                    DefinitionKey: c.DefinitionKey,
                    DisplayName: c.DisplayName,
                    Skills: c.Skills
                        .Select(s => new PlayerCharacterSkillView(s.SkillKey, s.UnlockedAtUtc, s.Source, s.IsEquipped))
                        .ToArray(),
                    Stats: new PlayerCharacterStatsView(
                        c.Stats.MaxVitality,
                        c.Stats.AttackPower,
                        c.Stats.Defense,
                        c.Stats.StartingGuard,
                        c.Stats.Speed,
                        c.Stats.Initiative,
                        c.Stats.Recovery,
                        c.Stats.Focus,
                        c.Stats.Mana,
                        c.Stats.Charge),
                    MaxEquippedSkills: c.MaxEquippedSkills))
                .ToArray(),
            Progression: new PlayerProgressionView(
                dto.Progression.UnspentStatPoints,
                dto.Progression.TotalStatPointsEarned));
    }

    private sealed record PlayerProfileResponse(
        Guid Id,
        string DisplayName,
        IReadOnlyCollection<PlayerCharacterResponse> Characters,
        PlayerProgressionResponse Progression);

    private sealed record PlayerCharacterResponse(
        Guid Id,
        string DefinitionKey,
        string DisplayName,
        IReadOnlyCollection<PlayerCharacterSkillResponse> Skills,
        PlayerCharacterStatsResponse Stats,
        int MaxEquippedSkills);

    private sealed record PlayerCharacterSkillResponse(
        string SkillKey,
        DateTimeOffset UnlockedAtUtc,
        string? Source,
        bool IsEquipped);

    private sealed record PlayerCharacterStatsResponse(
        int MaxVitality,
        int AttackPower,
        int Defense,
        int StartingGuard,
        int Speed,
        int Initiative,
        int Recovery,
        int Focus,
        int Mana,
        int Charge);

    private sealed record PlayerProgressionResponse(
        int UnspentStatPoints,
        int TotalStatPointsEarned);
}
