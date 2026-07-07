using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players;

public sealed class GetPlayerRunSnapshotQueryHandler
    : IRequestHandler<GetPlayerRunSnapshotQuery, PlayerRunSnapshotResponse>
{
    private readonly IPlayerProfileRepository _repository;

    public GetPlayerRunSnapshotQueryHandler(IPlayerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerRunSnapshotResponse> Handle(
        GetPlayerRunSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var playerId = new PlayerId(request.PlayerId);
        var profile = await _repository.GetByIdAsync(playerId, cancellationToken);

        if (profile is null)
        {
            throw new Common.Exceptions.NotFoundException("Player", request.PlayerId);
        }

        var availableCharacters = profile.Roster.GetAvailableCharacters();

        if (availableCharacters.Count == 0)
        {
            throw new Common.Exceptions.ConflictException("No available characters to start a run.");
        }

        return new PlayerRunSnapshotResponse(
            profile.Id.Value,
            profile.DisplayName,
            availableCharacters.Select(c => new PlayerRunSnapshotCharacterResponse(
                c.Id.Value,
                c.DefinitionKey,
                c.DisplayName,
                c.MaxVitality,
                c.BaseMana,
                c.BaseCharge,
                c.EquippedSkillKeys.ToArray(),
                new PlayerRunSnapshotCharacterStatsResponse(
                    c.StatBlock.MaxVitality,
                    c.StatBlock.AttackPower,
                    c.StatBlock.Defense,
                    c.StatBlock.StartingGuard,
                    c.StatBlock.Speed,
                    c.StatBlock.Initiative,
                    c.StatBlock.Recovery,
                    c.StatBlock.Focus,
                    c.StatBlock.Mana,
                    c.StatBlock.Charge),
                c.EquippedItemKeys.ToArray())).ToArray());
    }
}
