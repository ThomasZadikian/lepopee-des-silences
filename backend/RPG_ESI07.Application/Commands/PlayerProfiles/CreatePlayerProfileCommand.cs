using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerProfiles;

// Appelé par Unity après la création du compte
// Le joueur choisit son nom de personnage dans le jeu
public record CreatePlayerProfileCommand(
    int UserId,
    string CharacterName
) : IRequest<CreatePlayerProfileResponse>;

public record CreatePlayerProfileResponse(int Id, string Message);
