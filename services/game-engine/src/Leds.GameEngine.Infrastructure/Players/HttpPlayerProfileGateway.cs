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

    public async Task<PlayerProfileView> EquipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/items/{itemKey}/equip", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> UnequipItemAsync(Guid playerId, Guid characterId, string itemKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/items/{itemKey}/unequip", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> AddPermanentItemsAsync(Guid playerId, IReadOnlyCollection<string> itemDefinitionKeys, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/permanent-items",
            new AddPermanentItemsRequestBody(itemDefinitionKeys, sourceRunId), cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> SetPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, string liquidDefinitionKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/permanent-items/{itemDefinitionKey}/content",
            new SetPermanentItemContentRequestBody(liquidDefinitionKey), cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> ClearPermanentItemContentAsync(Guid playerId, string itemDefinitionKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/internal/players/{playerId}/permanent-items/{itemDefinitionKey}/content/clear", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> SpendStatPointAsync(Guid playerId, Guid characterId, string stat, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v2/players/{playerId}/characters/{characterId}/stats/{stat}/spend-point", content: null, cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken, string source = "devtools")
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/characters/{characterId}/skills/{skillKey}/unlock",
            new UnlockSkillRequestBody(source), cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> AwardStatPointsAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/stat-points/award", new AwardStatPointsRequestBody(amount), cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<PlayerProfileView> AwardCurrencyAsync(Guid playerId, int amount, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/currency/award", new AwardCurrencyRequestBody(amount), cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<bool> HasClaimedNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v2/internal/players/{playerId}/npcs/{npcKey}/offerings/{offeringKey}/claimed", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Player", playerId);
        }

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<HasClaimedNpcOfferingResponse>(cancellationToken);
        return dto?.Claimed ?? false;
    }

    public async Task ClaimNpcOfferingAsync(Guid playerId, string npcKey, string offeringKey, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/npcs/{npcKey}/offerings/{offeringKey}/claim",
            new SourceRunRequestBody(sourceRunId), cancellationToken);

        EnsureSuccess(response, playerId);
    }

    public async Task GrantReputationMilestoneAsync(Guid playerId, string npcKey, string milestoneKey, Guid? sourceRunId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/npcs/{npcKey}/reputation-milestones/{milestoneKey}",
            new SourceRunRequestBody(sourceRunId), cancellationToken);

        EnsureSuccess(response, playerId);
    }

    public async Task<PlayerProfileView> RecruitCompanionAsync(
        Guid playerId, string companionDefinitionKey, string displayName,
        int maxVitality, int attackPower, int defense, int startingGuard,
        int speed, int initiative, int recovery, int focus, int mana, int charge,
        IReadOnlyCollection<string> skillKeys, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/companions/{companionDefinitionKey}/recruit",
            new RecruitCompanionRequestBody(
                displayName, maxVitality, attackPower, defense, startingGuard,
                speed, initiative, recovery, focus, mana, charge, skillKeys),
            cancellationToken);

        return await ReadProfileAsync(response, playerId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<NpcReputationScoreView>> GetNpcReputationScoresAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/api/v2/internal/players/{playerId}/npc-reputation-scores", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("Player", playerId);

        response.EnsureSuccessStatusCode();

        var dtos = await response.Content.ReadFromJsonAsync<NpcReputationScoreResponse[]>(cancellationToken);
        return (dtos ?? []).Select(d => new NpcReputationScoreView(d.NpcKey, d.Score, d.TimesMet, d.CurrentDialogueNodeKey)).ToArray();
    }

    public async Task UpsertNpcReputationScoresAsync(Guid playerId, Guid sourceRunId, IReadOnlyCollection<NpcReputationScoreView> scores, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v2/internal/players/{playerId}/npc-reputation-scores",
            new UpsertNpcReputationScoresRequestBody(sourceRunId, scores.Select(s => new NpcReputationScoreRequestItem(s.NpcKey, s.Score, s.TimesMet, s.CurrentDialogueNodeKey)).ToArray()),
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("Player", playerId);

        response.EnsureSuccessStatusCode();
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
                    MaxEquippedSkills: c.MaxEquippedSkills,
                    Items: (c.Items ?? [])
                        .Select(i => new PlayerCharacterItemView(i.ItemKey, i.AcquiredAtUtc, i.Source, i.IsEquipped))
                        .ToArray(),
                    MaxEquippedItems: c.MaxEquippedItems,
                    CharacterType: c.CharacterType))
                .ToArray(),
            Progression: new PlayerProgressionView(
                dto.Progression.UnspentStatPoints,
                dto.Progression.TotalStatPointsEarned,
                dto.Progression.PalaceShardCount),
            PermanentItems: (dto.PermanentItems ?? [])
                .Select(i => new PlayerPermanentItemView(i.ItemDefinitionKey, i.SourceRunId, i.AcquiredAtUtc, i.ContainedLiquidDefinitionKey))
                .ToArray());
    }

    private sealed record AwardStatPointsRequestBody(int Amount);

    private sealed record AwardCurrencyRequestBody(int Amount);

    private sealed record RecruitCompanionRequestBody(
        string DisplayName,
        int MaxVitality,
        int AttackPower,
        int Defense,
        int StartingGuard,
        int Speed,
        int Initiative,
        int Recovery,
        int Focus,
        int Mana,
        int Charge,
        IReadOnlyCollection<string> SkillKeys);

    private sealed record UnlockSkillRequestBody(string Source);

    private sealed record SourceRunRequestBody(Guid? SourceRunId);

    private sealed record AddPermanentItemsRequestBody(IReadOnlyCollection<string> ItemDefinitionKeys, Guid? SourceRunId);

    private sealed record SetPermanentItemContentRequestBody(string LiquidDefinitionKey);

    private sealed record HasClaimedNpcOfferingResponse(bool Claimed);

    private sealed record PlayerProfileResponse(
        Guid Id,
        string DisplayName,
        IReadOnlyCollection<PlayerCharacterResponse> Characters,
        PlayerProgressionResponse Progression,
        IReadOnlyCollection<PlayerPermanentItemResponse>? PermanentItems = null);

    private sealed record PlayerCharacterResponse(
        Guid Id,
        string DefinitionKey,
        string DisplayName,
        IReadOnlyCollection<PlayerCharacterSkillResponse> Skills,
        PlayerCharacterStatsResponse Stats,
        int MaxEquippedSkills,
        IReadOnlyCollection<PlayerCharacterItemResponse>? Items = null,
        int MaxEquippedItems = 3,
        string CharacterType = "Standard");

    private sealed record PlayerCharacterSkillResponse(
        string SkillKey,
        DateTimeOffset UnlockedAtUtc,
        string? Source,
        bool IsEquipped);

    private sealed record PlayerCharacterItemResponse(
        string ItemKey,
        DateTimeOffset AcquiredAtUtc,
        string? Source,
        bool IsEquipped);

    private sealed record PlayerPermanentItemResponse(
        string ItemDefinitionKey,
        Guid? SourceRunId,
        DateTimeOffset AcquiredAtUtc,
        string? ContainedLiquidDefinitionKey = null);

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
        int TotalStatPointsEarned,
        int PalaceShardCount = 0);

    private sealed record NpcReputationScoreResponse(
        string NpcKey,
        int Score,
        int TimesMet,
        string? CurrentDialogueNodeKey);

    private sealed record UpsertNpcReputationScoresRequestBody(
        Guid SourceRunId,
        IReadOnlyCollection<NpcReputationScoreRequestItem> Scores);

    private sealed record NpcReputationScoreRequestItem(
        string NpcKey,
        int Score,
        int TimesMet,
        string? CurrentDialogueNodeKey);
}
