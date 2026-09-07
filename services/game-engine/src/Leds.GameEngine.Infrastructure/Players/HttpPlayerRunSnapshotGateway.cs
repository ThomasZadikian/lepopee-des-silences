using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Combats.Typing;
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
                    if (c.Stats is null)
                    {
                        throw new InvalidOperationException(
                            $"Player character '{c.DefinitionKey}' has no stat snapshot.");
                    }

                    if (c.SkillKeys is null || c.SkillKeys.Count == 0 || c.SkillKeys.Any(string.IsNullOrWhiteSpace))
                    {
                        throw new InvalidOperationException(
                            $"Player character '{c.DefinitionKey}' has no valid equipped skill keys.");
                    }

                    var stats = new PlayerRunSnapshotCharacterStats(
                        MaxVitality: c.Stats.MaxVitality,
                        AttackPower: c.Stats.AttackPower,
                        Defense: c.Stats.Defense,
                        StartingGuard: c.Stats.StartingGuard,
                        Speed: c.Stats.Speed,
                        Initiative: c.Stats.Initiative,
                        Focus: c.Stats.Focus,
                        Mana: c.Stats.Mana,
                        Charge: c.Stats.Charge,
                        MagicAttack: c.Stats.MagicAttack,
                        MagicDefense: c.Stats.MagicDefense,
                        Movement: c.Stats.Movement);

                    // Only the keys are owned by Player Service. Mechanics are resolved
                    // authoritatively from Catalog by PlayerSkillMerger before use.
                    var skills = c.SkillKeys.Select(key => new PlayerRunSnapshotCharacterSkill(
                        SkillDefinitionKey: key,
                        DisplayName: string.Empty,
                        SkillType: string.Empty,
                        TargetingMode: string.Empty,
                        EffectType: string.Empty,
                        ManaCost: 0,
                        ChargeCost: 0,
                        BasePower: 0)).ToArray();

                    return new PlayerRunSnapshotCharacter(
                        CharacterId: c.CharacterId,
                        DefinitionKey: c.DefinitionKey,
                        DisplayName: c.DisplayName,
                        Stats: stats,
                        Skills: skills,
                        EquippedItemKeys: c.EquippedItemKeys ?? [],
                        EquipmentLoadout: (c.EquipmentLoadout ?? [])
                            .Select(item => new PlayerRunSnapshotEquipment(
                                item.ItemInstanceId, item.ItemDefinitionKey, item.Position))
                            .ToArray());
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
        IReadOnlyCollection<string>? EquippedItemKeys = null,
        IReadOnlyCollection<PlayerRunSnapshotEquipmentResponse>? EquipmentLoadout = null);

    private sealed record PlayerRunSnapshotEquipmentResponse(
        Guid ItemInstanceId,
        string ItemDefinitionKey,
        string Position);

    private sealed record PlayerRunSnapshotCharacterStatsResponse(
        int MaxVitality,
        int AttackPower,
        int Defense,
        int StartingGuard,
        int Speed,
        int Initiative,
        int Focus,
        int Mana,
        int Charge,
        int MagicAttack = 0,
        int MagicDefense = 0,
        int Movement = 4);

}
