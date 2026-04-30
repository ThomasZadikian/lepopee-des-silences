using MediatR;

namespace RPG_ESI07.Application.Queries.PlayerProfiles;

// Appelé par le site Vue.js pour afficher le profil du joueur connecté
public record GetPlayerProfileByUserIdQuery(int UserId)
    : IRequest<GetPlayerProfileByUserIdResponse>;

public record GetPlayerProfileByUserIdResponse(
    int Id,
    int UserId,
    string CharacterName,
    int Level,
    int CurrentHP,
    int MaxHP,
    int CurrentMP,
    int MaxMP,
    int Strength,
    int Intelligence,
    int Speed,
    int Experience,
    int Gold,
    DateTime UpdatedAt,

    // Stats de combat associées (nullable si pas encore joué)
    int? TotalCombats,
    int? CombatsWon,
    int? CombatsLost,
    long? TotalDamageDealt,
    long? TotalDamageTaken,
    int? TotalPlaytimeMinutes,

    // Compteurs pour le dashboard
    int SavesCount,
    int InventoryCount,
    int SkillsCount,
    int BestiaryCount
);
