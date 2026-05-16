using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerProfiles;

public class CreatePlayerProfileHandler
    : IRequestHandler<CreatePlayerProfileCommand, CreatePlayerProfileResponse>
{
    private readonly IPlayerProfileRepository _repository;

    public CreatePlayerProfileHandler(IPlayerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreatePlayerProfileResponse> Handle(
        CreatePlayerProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Valeurs initiales du personnage — stats de départ équilibrées
        var profile = new Domain.Entities.PlayerProfile
        {
            UserId = request.UserId,
            CharacterName = request.CharacterName,
            Level = 1,
            CurrentHP = 100,
            MaxHP = 100,
            CurrentMP = 50,
            MaxMP = 50,
            Strength = 10,
            Intelligence = 10,
            Speed = 10,
            Experience = 0,
            Gold = 0,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.AddAsync(profile);

        return new CreatePlayerProfileResponse(
            profile.Id,
            "Personnage créé avec succès");
    }
}