using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.RGPD;

public class GetUserDataHandler : IRequestHandler<GetUserDataQuery, GetUserDataResponse>
{
    private readonly IUserRepository _repository;

    public GetUserDataHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetUserDataResponse> Handle(GetUserDataQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetWithAllDataAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("Utilisateur introuvable");
        var profile = user.PlayerProfile;
        return new GetUserDataResponse(
            UserId: user.Id,
            Username: user.Username,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            LastLoginIP: user.LastLoginIP,
            GameSaves: profile?.GameSaves.ToList() ?? new(),
            Inventory: profile?.Inventory.ToList() ?? new(),
            Skills: profile?.Skills.ToList() ?? new(),
            BestiaryUnlocks: profile?.BestiaryUnlocks.ToList() ?? new(),
            CombatStats: profile?.CombatStats
        );
    }
}