using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.RGPD;

public class GetUserDataHandler : IRequestHandler<GetUserDataQuery, GetUserDataResponse>
{
    private readonly IUserRepository _repository;

    public GetUserDataHandler(IUserRepository repository) => _repository = repository;

    public async Task<GetUserDataResponse> Handle(GetUserDataQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetWithAllDataAsync(request.UserId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable");
        var profile = user.PlayerProfile;

        return new GetUserDataResponse(
            UserId: user.Id,
            Username: user.Username,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            GameSaves: profile?.GameSaves.Select(g => new GameSaveDto(
                g.Id, g.CurrentZone, g.PositionX, g.PositionY,
                g.InventoryData, g.QuestFlags, g.SavedAt)).ToList() ?? new(),
            Inventory: profile?.Inventory.Select(i => new InventoryItemDto(
                i.Id, i.ItemId, i.Quantity)).ToList() ?? new(),
            Skills: profile?.Skills.Select(s => new SkillDto(
                s.Id, s.SkillId, s.Player.Level)).ToList() ?? new(),
            BestiaryUnlocks: profile?.BestiaryUnlocks.Select(b => new BestiaryUnlockDto(
                b.Id, b.EnemyId, b.UnlockedAt)).ToList() ?? new(),
            CombatStats: profile?.CombatStats is { } cs
                ? new CombatStatsDto(cs.TotalCombats, cs.CombatsWon, cs.CombatsLost,
                    cs.TotalDamageDealt, cs.TotalDamageTaken, cs.TotalPlaytimeMinutes)
                : null);
    }
}