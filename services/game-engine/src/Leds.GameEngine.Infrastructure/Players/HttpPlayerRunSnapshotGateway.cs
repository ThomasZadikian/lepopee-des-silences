using System.Net.Http.Json;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;

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
                .Select(c => new PlayerRunSnapshotCharacter(
                    CharacterId: c.CharacterId,
                    DefinitionKey: c.DefinitionKey,
                    DisplayName: c.DisplayName,
                    MaxVitality: c.MaxVitality,
                    BaseMana: c.BaseMana,
                    BaseCharge: c.BaseCharge,
                    SkillKeys: c.SkillKeys))
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
        IReadOnlyCollection<string> SkillKeys);
}
