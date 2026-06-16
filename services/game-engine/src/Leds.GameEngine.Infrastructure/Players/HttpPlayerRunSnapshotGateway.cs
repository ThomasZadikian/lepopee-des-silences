using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using System.Net.Http.Json;

namespace Leds.GameEngine.Infrastructure.Players;

public sealed class HttpPlayerRunSnapshotGateway : IPlayerRunSnapshotGateway
{
    private readonly HttpClient _httpClient;

    public HttpPlayerRunSnapshotGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PlayerRunSnapshot> GetRunSnapshotAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v2/players/{playerId}/run-snapshot", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Player", playerId);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var conflictBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException(conflictBody);
        }

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PlayerRunSnapshotResponse>(cancellationToken);

        if (dto is null)
        {
            throw new InvalidOperationException("Player Service returned an empty snapshot.");
        }

        return new PlayerRunSnapshot(
            PlayerId: dto.PlayerId,
            DisplayName: dto.DisplayName,
            Characters: dto.Characters
                .Select(c =>
                {
                    var stats = c.Stats is not null
                        ? new PlayerRunSnapshotCharacterStats(
                            MaxVitality: c.Stats.MaxVitality,
                            AttackPower: c.Stats.AttackPower,
                            Defense: c.Stats.Defense,
                            StartingGuard: c.Stats.StartingGuard,
                            Speed: c.Stats.Speed,
                            Initiative: c.Stats.Initiative,
                            Recovery: c.Stats.Recovery,
                            Focus: c.Stats.Focus,
                            Mana: c.Stats.Mana,
                            Charge: c.Stats.Charge)
                        : new PlayerRunSnapshotCharacterStats(
                            MaxVitality: c.MaxVitality,
                            AttackPower: 12,
                            Defense: 6,
                            StartingGuard: 0,
                            Speed: 10,
                            Initiative: 10,
                            Recovery: 5,
                            Focus: 0,
                            Mana: c.BaseMana,
                            Charge: c.BaseCharge);

                    var skills = c.Skills is { Count: > 0 }
                        ? c.Skills.Select(s => new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: s.SkillDefinitionKey,
                            DisplayName: s.DisplayName,
                            SkillType: s.SkillType,
                            TargetingMode: s.TargetingMode,
                            EffectType: s.EffectType,
                            ManaCost: s.ManaCost,
                            ChargeCost: s.ChargeCost,
                            BasePower: s.BasePower)).ToArray()
                        : c.SkillKeys.Select(key => new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: key,
                            DisplayName: key,
                            SkillType: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Defense" : "Damage",
                            TargetingMode: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Self" : "SingleEnemy",
                            EffectType: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Guard" : "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? 5 : 10)).ToArray();

                    return new PlayerRunSnapshotCharacter(
                        CharacterId: c.CharacterId,
                        DefinitionKey: c.DefinitionKey,
                        DisplayName: c.DisplayName,
                        Stats: stats,
                        Skills: skills);
                })
                .ToArray());
    }

    private sealed record PlayerRunSnapshotResponse(
        Guid PlayerId,
        string DisplayName,
        IReadOnlyCollection<PlayerRunSnapshotCharacterResponse> Characters);

    private sealed record PlayerRunSnapshotCharacterResponse(
        Guid CharacterId,
        string DefinitionKey,
        string DisplayName,
        int MaxVitality,
        int BaseMana,
        int BaseCharge,
        IReadOnlyCollection<string> SkillKeys,
        PlayerRunSnapshotCharacterStatsResponse? Stats = null,
        IReadOnlyCollection<PlayerRunSnapshotCharacterSkillResponse>? Skills = null);

    private sealed record PlayerRunSnapshotCharacterStatsResponse(
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

    private sealed record PlayerRunSnapshotCharacterSkillResponse(
        string SkillDefinitionKey,
        string DisplayName,
        string SkillType,
        string TargetingMode,
        string EffectType,
        int ManaCost,
        int ChargeCost,
        int BasePower);
}
