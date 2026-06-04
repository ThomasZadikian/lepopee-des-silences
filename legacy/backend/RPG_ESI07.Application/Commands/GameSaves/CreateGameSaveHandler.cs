using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.GameSaves;

public class CreateGameSaveHandler : IRequestHandler<CreateGameSaveCommand, CreateGameSaveResponse>
{
    private readonly IGameSaveRepository _repository;

    public CreateGameSaveHandler(IGameSaveRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateGameSaveResponse> Handle(CreateGameSaveCommand request, CancellationToken cancellationToken)
    {
        var entity = new GameSave();
        await _repository.AddAsync(entity);

        return new CreateGameSaveResponse(entity.Id, "GameSave created successfully");
    }
}