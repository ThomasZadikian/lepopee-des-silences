using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerProfiles;

// Appelé par Unity à chaque sauvegarde
// Contient l'état complet du personnage et de la partie
public record SyncGameSaveCommand(
    int RequestingUserId,

    // État de la partie
    string CurrentZone,
    float PositionX,
    float PositionY,
    string? QuestFlags,

    // État du personnage
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

    // Stats de combat de la session
    int TotalCombats,
    int CombatsWon,
    int CombatsLost,
    long TotalDamageDealt,
    long TotalDamageTaken,
    int TotalPlaytimeMinutes
) : IRequest<SyncGameSaveResponse>;

public record SyncGameSaveResponse(bool Success, string Message);
