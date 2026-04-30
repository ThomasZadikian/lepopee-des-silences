using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.PlayerProfiles;

public class GetPlayerProfileByUserIdHandler
    : IRequestHandler<GetPlayerProfileByUserIdQuery, GetPlayerProfileByUserIdResponse>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly IUserRepository _userRepository;

    public GetPlayerProfileByUserIdHandler(
        IPlayerProfileRepository repository,
        IUserRepository userRepository)
    {
        _repository     = repository;
        _userRepository = userRepository;
    }

    public async Task<GetPlayerProfileByUserIdResponse> Handle(
        GetPlayerProfileByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        // Récupérer l'utilisateur avec toutes ses données liées
        var user = await _userRepository.GetWithAllDataAsync(request.UserId);

        if (user == null)
            throw new KeyNotFoundException("Utilisateur introuvable");

        var profile = user.PlayerProfile;

        if (profile == null)
            throw new KeyNotFoundException(
                "Aucun personnage trouvé. Le personnage doit être créé dans le jeu.");

        var stats = profile.CombatStats;

        return new GetPlayerProfileByUserIdResponse(
            Id:                   profile.Id,
            UserId:               profile.UserId,
            CharacterName:        profile.CharacterName,
            Level:                profile.Level,
            CurrentHP:            profile.CurrentHP,
            MaxHP:                profile.MaxHP,
            CurrentMP:            profile.CurrentMP,
            MaxMP:                profile.MaxMP,
            Strength:             profile.Strength,
            Intelligence:         profile.Intelligence,
            Speed:                profile.Speed,
            Experience:           profile.Experience,
            Gold:                 profile.Gold,
            UpdatedAt:            profile.UpdatedAt,

            // Stats de combat
            TotalCombats:         stats?.TotalCombats,
            CombatsWon:           stats?.CombatsWon,
            CombatsLost:          stats?.CombatsLost,
            TotalDamageDealt:     stats?.TotalDamageDealt,
            TotalDamageTaken:     stats?.TotalDamageTaken,
            TotalPlaytimeMinutes: stats?.TotalPlaytimeMinutes,

            // Compteurs pour le dashboard
            SavesCount:           profile.GameSaves.Count,
            InventoryCount:       profile.Inventory.Count,
            SkillsCount:          profile.Skills.Count,
            BestiaryCount:        profile.BestiaryUnlocks.Count
        );
    }
}
